using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using File = System.IO.File;
using Directory = System.IO.Directory;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO.Compression;
using UnityEngine.Windows;
using UnityEditor.Build;
using UnityEditor.Search;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace ImmerzaSDK.Manager.Editor
{
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

    public partial class SDKManagerWindow : EditorWindow
    {
        private static readonly ReleaseInfo InvalidRelease = new ReleaseInfo(string.Empty, "None", string.Empty, 0);

        [SerializeField]
        private VisualTreeAsset _treeAssetMainPage = null;
        [SerializeField]
        private VisualTreeAsset _treeAssetAuthPage = null;
        [SerializeField]
        private Sprite _logo = null;

        #region Auth UI Elements
        private TextField _emailField = null;
        private TextField _passwordField = null;
        private Label _authMessage = null;
        private Button _signInButton = null;
        #endregion

        #region Main Page UI Elements
        private Label _crtVersionField = null;
        private Button _refreshBtn = null;
        private Button _updateBtn = null;
        private Button _logoutBtn = null;
        private ProgressBar _progressBar = null;
        private Label _successLabel = null;
        private Label _releaseNotes = null;
        #endregion

        #region Account Page UI Elements
        private Label _userNameLabel = null;
        private Label _userMailLabel = null;
        #endregion

        #region Status Page UI Elements
        private GroupBox _errorBox = null;
        private GroupBox _warningBox = null;
        private Label _warningCountLabel = null;
        private Label _errorCountLabel = null;
        #endregion

        private ReleaseInfo _currentReleaseInfo;
        private AuthData _authData;

        private int _errorCount = 0;
        private int _warningCount = 0;

        [MenuItem("Immerza/SDK Manager")]
        public static void ShowWindow()
        {
            GetWindow<SDKManagerWindow>("Immerza SDK Manager");
        }

        private void clearVisualRoot()
        {
            rootVisualElement.Clear();
        }

        private void initializeAuthPage()
        {
            clearVisualRoot();

            _treeAssetAuthPage.CloneTree(rootVisualElement);
            VisualElement authRoot = rootVisualElement.Q<VisualElement>("AuthPage");

            Image immerzaLogo = new()
            {
                scaleMode = ScaleMode.ScaleToFit,
                sprite = _logo
            };
            immerzaLogo.style.marginTop = 20.0f;
            immerzaLogo.style.marginLeft = 10.0f;
            immerzaLogo.style.marginRight = 10.0f;
            immerzaLogo.style.marginBottom = 20.0f;

            authRoot.Insert(0, immerzaLogo);

            _emailField = authRoot.Q<TextField>("EmailField");
            _passwordField = authRoot.Q<TextField>("PasswordField");
            _authMessage = authRoot.Q<Label>("AuthMessage");
            _signInButton = authRoot.Q<Button>("SignInButton");

            _signInButton.clicked += HandleSignIn;

            EventCallback<KeyDownEvent> onKeyDown = (KeyDownEvent ev) =>
            {
                if (ev.keyCode == KeyCode.Return)
                {
                    HandleSignIn();
                }
            };
            _emailField.RegisterCallback<KeyDownEvent>(onKeyDown, TrickleDown.TrickleDown);
            _passwordField.RegisterCallback<KeyDownEvent>(onKeyDown, TrickleDown.TrickleDown);
        }

        private async Awaitable initializeMainPage()
        {
            clearVisualRoot();

            _treeAssetMainPage.CloneTree(rootVisualElement);
            VisualElement mainPageRoot = rootVisualElement.Q<VisualElement>("MainPage");

            _errorBox = mainPageRoot.Q<GroupBox>("ErrorBox");
            _warningBox = mainPageRoot.Q<GroupBox>("WarningsBox");
            _warningCountLabel = mainPageRoot.Q<Label>("WarningsCount");
            _errorCountLabel = mainPageRoot.Q<Label>("ErrorsCount");

            _crtVersionField = mainPageRoot.Q<Label>("CurrentVersion");
            _releaseNotes = mainPageRoot.Q<Label>("ReleaseNotes");
            _refreshBtn = mainPageRoot.Q<Button>("RefreshButton");
            _updateBtn = mainPageRoot.Q<Button>("UpdateButton");
            _logoutBtn = mainPageRoot.Q<Button>("LogoutButton");
            _successLabel = mainPageRoot.Q<Label>("SuccessLabel");
            _successLabel.visible = false;

            _progressBar = mainPageRoot.Q<ProgressBar>("DownloadProgress");
            _refreshBtn.clicked += async () => await CheckForNewSdkVersion();
            _updateBtn.clicked += async () => await InstallOrUpdateSdk();

            _userNameLabel = mainPageRoot.Q<Label>("UserNameLabel");
            _userNameLabel.text = _authData.User.Name;
            _userMailLabel = mainPageRoot.Q<Label>("UserMailLabel");
            _userMailLabel.text = _authData.User.Mail;

            _logoutBtn.clicked += Logout;

            InitializeDeployView(mainPageRoot.Q<VisualElement>("DeployPage"));

#if IMMERZA_SDK_INSTALLED
            PreflightCheckManager.OnLogCheck += HandleNewCheckResults;
            PreflightCheckManager.OnBeforeRunChecks += OnBeforeRunChecks;
            DispatchChecks();
#endif

            await CheckForNewSdkVersion();
        }

        public async void CreateGUI()
        {
            _authData = await Auth.Setup();
            if (_authData != null)
            {
                await initializeMainPage();
            }
            else
            {
                initializeAuthPage();
            }
        }

        private async void HandleSignIn()
        {
            if (string.IsNullOrEmpty(_emailField.text) || string.IsNullOrEmpty(_passwordField.text))
            {
                SetLabelMsg(_authMessage, false, "Please provide both the email address and password to your Immerza account.");
                return;
            }

            _signInButton.SetEnabled(false);
            _authMessage.visible = false;

            string signInMsg;
            (_authData, signInMsg) = await Auth.SignIn(_emailField.text, _passwordField.text);

            if (_authData == null)
            {
                SetLabelMsg(_authMessage, false, signInMsg);
                _signInButton.SetEnabled(true);
                return;
            }

            await initializeMainPage();
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
                    catch
                    {
                    }
                }
            }

            return date != DateTime.MinValue;
        }

        private async Awaitable CheckForNewSdkVersion()
        {
            _currentReleaseInfo = await GetLatestReleaseInfo();
            
            if (LoadInstalledVersionInfo(out string installedVersion, out DateTime installedVersionDate))
            {
                _updateBtn.text = $"Update to {_currentReleaseInfo.Version}";
                _crtVersionField.text = $"{installedVersion} · {installedVersionDate.ToString("MMMM dd, yyyy")}";
            }
            else
            {
                _updateBtn.text = $"Install {_currentReleaseInfo.Version}";
                _crtVersionField.text = "N/A";
            }

            bool updateAvailable = IsNewerThanInstalledRelease(_currentReleaseInfo, installedVersion);
            _releaseNotes.text = _currentReleaseInfo.Changelog;
            _updateBtn.style.display = updateAvailable ? DisplayStyle.Flex : DisplayStyle.None; 
        }

        private void Logout()
        {
            Auth.ClearLogoutData();
            _authData = Auth.InvalidAuthData;
            initializeAuthPage();
        }

        private async Task<ReleaseInfo> GetLatestReleaseInfo()
        {
            if (!await Auth.CheckAuthData(_authData))
            {
                return InvalidRelease;
            }

            using UnityWebRequest releasesReq = UnityWebRequest.Get(Constants.API_ROUTE_RELEASES);
            releasesReq.SetRequestHeader("Authorization", _authData.AccessToken);
            releasesReq.SetRequestHeader("Accept", "application/json");
            await releasesReq.SendWebRequest();

            if (releasesReq.result != UnityWebRequest.Result.Success)
            {
                SetLabelMsg(_successLabel, false, "Network Error, couldn't get releases.");
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
            catch (ArgumentException)
            {
            }

            return releaseInfo;
        }

        private static bool IsNewerThanInstalledRelease(ReleaseInfo releaseInfo, string installedVersion)
        {
            if (releaseInfo.Equals(InvalidRelease) || string.IsNullOrEmpty(installedVersion))
            {
                return true;
            }
            
            return CompareVersions(installedVersion, releaseInfo.Version) < 0;
        }

        private async Awaitable InstallOrUpdateSdk()
        {
            if (!await Auth.CheckAuthData(_authData))
            {
                return;
            }

            using UnityWebRequest req = UnityWebRequest.Get(Constants.API_ROUTE_FILES + _currentReleaseInfo.Url);
            req.SetRequestHeader("Authorization", _authData.AccessToken);
            UnityWebRequestAsyncOperation op = req.SendWebRequest();

            _progressBar.visible = true;
            while (!op.isDone)
            {
                _progressBar.value = op.progress;
                await Task.Delay(5);
            }
            _progressBar.value = 1.0f;
            _progressBar.visible = false;

            if (req.result != UnityWebRequest.Result.Success)
            {
                return;
            }

            if (Directory.Exists(Constants.SDK_BASE_PATH))
            {
                DirectoryInfo dirInfo = new(Constants.SDK_BASE_PATH);
                dirInfo.Delete(true);
            }

            ExtractZipContents(req.downloadHandler.data, Constants.SDK_BASE_PATH);

            File.WriteAllText(Path.Combine(Constants.SDK_BASE_PATH, "Version.txt"), $"{_currentReleaseInfo.Version} {_currentReleaseInfo.Date}");
            File.Copy(Path.Combine(Constants.SDK_BASE_PATH, "XLua", "Gen", "link.xml"), Path.Combine(Application.dataPath, "link.xml"), true);

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, "IMMERZA_SDK_INSTALLED");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "IMMERZA_SDK_INSTALLED");

            AssetDatabase.Refresh();

            await CheckForNewSdkVersion();
        }

        private void SetButton(Button button, bool activate)
        {
            if (activate)
            {
                button.SetEnabled(true);
                button.style.backgroundColor = new UnityEngine.Color(0.4f, 0.4f, 0.4f);
                button.style.color = new UnityEngine.Color(1.0f, 1.0f, 1.0f);
            }
            else
            {
                button.SetEnabled(false);
                button.style.backgroundColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f);
                button.style.color = new UnityEngine.Color(0.3f, 0.3f, 0.3f);
            }
        }

        private void SetLabelMsg(Label label, bool success, string message)
        {
            label.visible = true;
            if (success)
            {
                label.style.color = new Color(0.36f, 1.0f, 0.36f);
            }
            else
            {
                label.style.color = new Color(1.0f, 0.36f, 0.36f);
            }

            label.text = message;
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

        private void ExtractZipContents(byte[] data, string extractPath)
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

        #if IMMERZA_SDK_INSTALLED
        private void DispatchChecks()
        {
            PreflightCheckManager.RunChecks();
        }

        private void HandleNewCheckResults(ResultType type, string message)
        {
            if (type == ResultType.Error)
            {
                _errorCountLabel.text = Convert.ToString(++_errorCount);
                Label newMsg = new(message);
                newMsg.AddToClassList("label-wrap");
                _errorBox.Add(newMsg);
            }
            else if (type == ResultType.Warning)
            {
                _warningCountLabel.text = Convert.ToString(++_warningCount);
                _warningBox.Add(new Label(message));
            }
        }

        private void OnBeforeRunChecks()
        {
            _warningCount = 0;
            _errorCount = 0;
            _errorBox.Clear();
            _warningBox.Clear();
            _warningCountLabel.text = "0";
            _errorCountLabel.text = "0";
        }
#endif
    }
}
