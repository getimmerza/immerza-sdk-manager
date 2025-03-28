using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Unity.Android.Gradle.Manifest;
using System.Threading.Tasks;

namespace ImmerzaSDK.Manager.Editor
{
    struct Release
    {
        public Release(string version, string url)
        {
            Version = version;
            Url = url;
        }

        public string Version { get; }
        public string Url { get; }
    }

    public class SDKManagerWindow : EditorWindow
    {
        private readonly Release InvalidRelease = new Release(string.Empty, "None");

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

#region Update UI Elements
        private Label _crtVersionField = null;
        private Label _crtNewVersionField = null;
        private Button _refreshBtn = null;
        private Button _updateBtn = null;
        private Button _logoutBtn = null;
        private Label _successLabel = null;
#endregion
        
        private Release _currentRelease;
        private string _installedVersion = string.Empty;
        private AuthData _authData;

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
        }

        private async Awaitable initializeMainPage()
        {
            clearVisualRoot();

            _treeAssetMainPage.CloneTree(rootVisualElement);
            VisualElement mainPageRoot = rootVisualElement.Q<VisualElement>("MainPage");

            _crtVersionField = mainPageRoot.Q<Label>("CurrentVersion");
            _crtNewVersionField = mainPageRoot.Q<Label>("NewVersion");
            _refreshBtn = mainPageRoot.Q<Button>("RefreshButton");
            _updateBtn = mainPageRoot.Q<Button>("UpdateButton");
            _logoutBtn = mainPageRoot.Q<Button>("LogoutButton");
            _successLabel = mainPageRoot.Q<Label>("SuccessLabel");
            _successLabel.visible = false;

            _refreshBtn.clicked += async () => await CheckForNewSdkVersion();
            _logoutBtn.clicked += Logout;

            TextAsset installedVersion = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Immerza/Version.txt");
            if (installedVersion != null)
            {
                _installedVersion = installedVersion.text;
            }

            _crtVersionField.text = string.IsNullOrEmpty(_installedVersion) ? "None" : _installedVersion;

            await CheckForNewSdkVersion();
        }

        public async void CreateGUI()
        {
            VisualElement root = rootVisualElement;

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

        private async Awaitable CheckForNewSdkVersion()
        {
            SetButton(_updateBtn, false);
            _crtNewVersionField.text = string.Empty;

            _currentRelease = await GetReleases();
            if (IsNewThanInstalledRelease(_currentRelease))
            {
                SetButton(_updateBtn, true);
                _crtNewVersionField.text = string.Format("Update {0}", _currentRelease.Version);
            }
        }

        private void Logout()
        {
            Auth.ClearLogoutData();
            _authData = Auth.InvalidAuthData;
            initializeAuthPage();
        }

        private async Task<Release> GetReleases()
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

            Release releaseInfo = InvalidRelease;

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

                releaseInfo = new Release(version, fileId);

            }
            catch (ArgumentException)
            {
            }

            return releaseInfo;
        }

        private bool IsNewThanInstalledRelease(Release releaseInfo)
        {
            if (releaseInfo.Equals(InvalidRelease) || string.IsNullOrEmpty(_installedVersion))
            {
                return true;
            }

            return CompareVersions(_installedVersion, releaseInfo.Version) > 0;
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

        private int CompareVersions(string version1, string version2)
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
    }
}
