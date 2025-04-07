using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.Build;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace ImmerzaSDK.Manager.Editor
{
    public partial class SDKManagerWindow
    {
        private static readonly ReleaseInfo InvalidRelease = new ReleaseInfo(string.Empty, "None", string.Empty, 0);

        #region UI Elements
        private Label _pageUpdateLblCrtVersion;
        private Button _pageUpdateBtnRefresh;
        private Button _pageUpdateBtnUpdate;
        private ProgressBar _pageUpdatePrgbProgress;
        private Label _pageUpdateLblSuccess;
        private Label _pageUpdateLblReleaseNotes;
        #endregion

        private ReleaseInfo _currentReleaseInfo;

        private async Task InitializeUpdateView(VisualElement pageRoot)
        {
            _pageUpdateLblCrtVersion = pageRoot.Q<Label>("CurrentVersion");
            _pageUpdateLblReleaseNotes = pageRoot.Q<Label>("ReleaseNotes");
            _pageUpdateBtnRefresh = pageRoot.Q<Button>("RefreshButton");
            _pageUpdateBtnUpdate = pageRoot.Q<Button>("UpdateButton");
            _pageUpdateLblSuccess = pageRoot.Q<Label>("SuccessLabel");
            _pageUpdateLblSuccess.visible = false;

            _pageUpdatePrgbProgress = pageRoot.Q<ProgressBar>("DownloadProgress");
            _pageUpdateBtnRefresh.clicked += async () => await CheckForNewSdkVersion();
            _pageUpdateBtnUpdate.clicked += async () => await InstallOrUpdateSdk();

            await CheckForNewSdkVersion();
        }

        private bool LoadInstalledVersionInfo(out string version, out DateTime date)
        {
            version = string.Empty;
            date = DateTime.MinValue;

            string versionInfo = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Immerza/Version.txt")?.text ?? string.Empty;
            if (!string.IsNullOrEmpty(versionInfo))
            {
                string[] parts = versionInfo.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                {
                    version = parts[0];
                    try
                    {
                        if (long.TryParse(parts[1], out long timestamp))
                            date = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    }
                    catch (Exception e)
                    {
                        Log.LogError(e.ToString(), LogChannelType.SDKManager);
                    }
                }
            }

            return date != DateTime.MinValue;
        }

        private async Awaitable CheckForNewSdkVersion()
        {
            Log.LogInfo("Checking for SDK updates", LogChannelType.SDKManager);

            _currentReleaseInfo = await GetLatestReleaseInfo();

            Log.LogInfo($"Found {_currentReleaseInfo.Version}", LogChannelType.SDKManager);

            if (LoadInstalledVersionInfo(out string installedVersion, out DateTime installedVersionDate))
            {
                _pageUpdateBtnUpdate.text = $"Update to {_currentReleaseInfo.Version}";
                _pageUpdateLblCrtVersion.text = $"{installedVersion} · {installedVersionDate.ToString("MMMM dd, yyyy")}";
            }
            else
            {
                _pageUpdateBtnUpdate.text = $"Install {_currentReleaseInfo.Version}";
                _pageUpdateLblCrtVersion.text = "N/A";
            }

            bool updateAvailable = IsNewerThanInstalledRelease(_currentReleaseInfo, installedVersion);
            _pageUpdateLblReleaseNotes.text = _currentReleaseInfo.Changelog;
            _pageUpdateBtnUpdate.style.display = updateAvailable ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private async Task<ReleaseInfo> GetLatestReleaseInfo()
        {
            if (!await Auth.CheckAuthData(_authData))
            {
                Log.LogError("Refreshing access token failed...", LogChannelType.SDKManager);
                return InvalidRelease;
            }

            using UnityWebRequest releasesReq = UnityWebRequest.Get(Constants.API_ROUTE_RELEASES);
            releasesReq.SetRequestHeader("Authorization", _authData.AccessToken);
            releasesReq.SetRequestHeader("Accept", "application/json");
            await releasesReq.SendWebRequest();

            if (releasesReq.result != UnityWebRequest.Result.Success)
            {
                SetLabelMsg(_pageUpdateLblSuccess, false, "Network Error, couldn't get releases.");
                Log.LogError($"Request failed with '{releasesReq.result}': {releasesReq.error}", LogChannelType.SDKManager);
                return InvalidRelease;
            }

            ReleaseInfo releaseInfo = InvalidRelease;

            try
            {
                JObject release = JObject.Parse(releasesReq.downloadHandler.text);
                JToken firstEntry = release["entry"][0];
                JToken versionInfo = firstEntry["resource"]["extension"];

                string version = string.Empty;
                string fileId = string.Empty;
                string changelog = string.Empty;
                foreach (JToken metaData in versionInfo.ToArray())
                {
                    string identifier = metaData["url"].Value<string>() ?? string.Empty;
                    string value = metaData["valueString"].Value<string>() ?? string.Empty;
                    switch (identifier)
                    {
                        case "version":
                            version = value;
                            break;
                        case "fileId":
                            fileId = value;
                            break;
                        case "changelog":
                            changelog = value;
                            break;
                    }
                }

                JToken versionInfoLastUpdate = firstEntry["resource"]["meta"]["lastUpdated"];
                DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(versionInfoLastUpdate.Value<string>() ?? string.Empty);
                releaseInfo = new ReleaseInfo(version, fileId, changelog, dateTimeOffset.ToUnixTimeSeconds());

            }
            catch (ArgumentException e)
            {
                Log.LogError($"Malformed response: {e.Message}", LogChannelType.SDKManager);
            }

            return releaseInfo;
        }

        private async Awaitable InstallOrUpdateSdk()
        {
            if (!await Auth.CheckAuthData(_authData))
            {
                Log.LogError("Refreshing access token failed...", LogChannelType.SDKManager);
                return;
            }

            using UnityWebRequest req = UnityWebRequest.Get(Constants.API_ROUTE_FILES + _currentReleaseInfo.Url);
            req.SetRequestHeader("Authorization", _authData.AccessToken);
            UnityWebRequestAsyncOperation op = req.SendWebRequest();

            _pageUpdatePrgbProgress.visible = true;
            while (!op.isDone)
            {
                _pageUpdatePrgbProgress.value = op.progress;
                await Task.Delay(5);
            }
            _pageUpdatePrgbProgress.value = 1.0f;
            _pageUpdatePrgbProgress.visible = false;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Log.LogError($"Request failed with '{req.result}': {req.error}", LogChannelType.SDKManager);
                return;
            }

            Log.LogInfo("Installing SDK content...", LogChannelType.SDKManager);

            if (Directory.Exists(Constants.SDK_BASE_PATH))
            {
                Log.LogInfo("\tremoving previous SDK folder", LogChannelType.SDKManager);
                DirectoryInfo dirInfo = new(Constants.SDK_BASE_PATH);
                dirInfo.Delete(true);
            }

            Log.LogInfo("\textractng SDK package", LogChannelType.SDKManager);
            ExtractZipContents(req.downloadHandler.data, Constants.SDK_BASE_PATH);

            Log.LogInfo("\tpost setup steps", LogChannelType.SDKManager);
            File.WriteAllText(Path.Combine(Constants.SDK_BASE_PATH, "Version.txt"), $"{_currentReleaseInfo.Version} {_currentReleaseInfo.Date}");
            File.Copy(Path.Combine(Constants.SDK_BASE_PATH, "XLua", "Gen", "link.xml"), Path.Combine(Application.dataPath, "link.xml"), true);

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, "IMMERZA_SDK_INSTALLED");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "IMMERZA_SDK_INSTALLED");

            AssetDatabase.Refresh();

            Log.LogInfo($"SDK updated to {_currentReleaseInfo.Version}", LogChannelType.SDKManager);

            await CheckForNewSdkVersion();
        }

        private static bool IsNewerThanInstalledRelease(ReleaseInfo releaseInfo, string installedVersion)
        {
            if (releaseInfo.Equals(InvalidRelease) || string.IsNullOrEmpty(installedVersion))
            {
                return true;
            }

            return CompareVersions(installedVersion, releaseInfo.Version) < 0;
        }

        private static int CompareVersions(string version1, string version2)
        {
            Regex regex = new(@"^(?:v)?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9]+))?$");

            var match1 = regex.Match(version1);
            var match2 = regex.Match(version2);

            if (!match1.Success || !match2.Success)
            {
                throw new ArgumentException("Invalid version format!");
            }

            int major1 = int.Parse(match1.Groups[1].Value);
            int minor1 = int.Parse(match1.Groups[2].Value);
            int patch1 = int.Parse(match1.Groups[3].Value);
            string preRelease1 = match1.Groups[4].Success ? match1.Groups[4].Value : "";

            int major2 = int.Parse(match2.Groups[1].Value);
            int minor2 = int.Parse(match2.Groups[2].Value);
            int patch2 = int.Parse(match2.Groups[3].Value);
            string preRelease2 = match2.Groups[4].Success ? match2.Groups[4].Value : "";

            if (major1 != major2) return major1.CompareTo(major2);
            if (minor1 != minor2) return minor1.CompareTo(minor2);
            if (patch1 != patch2) return patch1.CompareTo(patch2);

            if (string.IsNullOrEmpty(preRelease1) && !string.IsNullOrEmpty(preRelease2)) return 1;
            if (!string.IsNullOrEmpty(preRelease1) && string.IsNullOrEmpty(preRelease2)) return -1;

            return string.Compare(preRelease1, preRelease2, StringComparison.Ordinal);
        }

        private static void ExtractZipContents(byte[] data, string extractPath)
        {
            using MemoryStream stream = new MemoryStream(data);
            using ZipArchive archive = new ZipArchive(stream);

            if (!Directory.Exists(extractPath))
            {
                Directory.CreateDirectory(extractPath);
            }

            int topLevelFolders = archive.Entries
                .Select(e => e.FullName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
                .Where(f => f != null)
                .Distinct()
                .ToList().Count;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string entryPath = entry.FullName;

                string fullPath = Path.Combine(extractPath, entryPath.Substring(entryPath.IndexOf('/') + topLevelFolders == 1 ? 1 : 0));

                if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                {
                    Directory.CreateDirectory(fullPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    entry.ExtractToFile(fullPath, overwrite: true);
                }
            }
        }
    }

    internal struct ReleaseInfo
    {
        public ReleaseInfo(string version, string url, string changelog, long date)
        {
            Version = version;
            Url = url;
            Changelog = changelog;
            Date = date;
        }

        public string Version { get; }
        public string Url { get; }
        public string Changelog { get; }
        public long Date { get; }
    }
}
