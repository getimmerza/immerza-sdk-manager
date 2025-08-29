using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace ImmerzaSDK.Manager.Editor
{
    public partial class SDKManagerWindow
    {
        #region UI Elements
        private ListView _pageDeployLstScenes;
        private TextField _pageDeployTxtPath;
        private TextField _pageDeployTxtExperienceName;
        private Button _pageDeployBtnExport;
        private Button _pageDeployBtnRunLocal;
        private Button _pageDeployBtnUpload;
        private Button _pageDeployBtnRefresh;
        private Toggle _pageDeployTglOpenExportFolder;
        private Label _pageDeployLblSuccess;
        private Label _pageDeployLblAction;
        private TabView _pageDeployParenTabView;
        #endregion

        static readonly string BUILD_ARTIFACT_BUNDLE_WIN = "immerza_scene_win.bundle";
        static readonly string BUILD_ARTIFACT_BUNDLE_ANDROID = "immerza_scene_android.bundle";
        static readonly string BUILD_ARTIFACT_METADATA = "immerza_metadata.json";

        private SceneAsset _sceneToExport;

        private void InitializeDeployView(TabView parentTabView, VisualElement pageRoot)
        {
            _pageDeployLstScenes = pageRoot.Q<ListView>("SceneList");
            _pageDeployLstScenes.selectionChanged += SceneSelected;

            _pageDeployTxtPath = pageRoot.Q<TextField>("ExportPath");
            _pageDeployTxtExperienceName = pageRoot.Q<TextField>("ExperienceName");

            _pageDeployBtnExport = pageRoot.Q<Button>("ExportButton");
            _pageDeployBtnExport.clicked += () => ExportScene();

            _pageDeployBtnRunLocal = pageRoot.Q<Button>("RunLocalButton");
            _pageDeployBtnRunLocal.clicked += ExportAndRunScene;

            _pageDeployBtnUpload = pageRoot.Q<Button>("UploadButton");
            _pageDeployBtnUpload.clicked += () => {
                try
                {
                    _pageDeployBtnUpload.SetEnabled(false);
                    UploadScene();
                }
                finally
                {
                    _pageDeployBtnUpload.SetEnabled(true);
                }
            };

            _pageDeployBtnRefresh = pageRoot.Q<Button>("RefreshButton");
            _pageDeployBtnRefresh.clicked += UpdateSceneList;

            _pageDeployTglOpenExportFolder = pageRoot.Q<Toggle>("OpenExportFolder");

            _pageDeployLblSuccess = pageRoot.Q<Label>("SuccessLabel");

            _pageDeployLblAction = pageRoot.Q<Label>("ActionLink");

            _pageDeployParenTabView = parentTabView;

            SetButtonEnabled(_pageDeployBtnExport, false);
            SetButtonEnabled(_pageDeployBtnRunLocal, false);

            UpdateSceneList();
            ResetDeployView();
        }

        private void ResetDeployView()
        {
            _pageDeployLblAction.visible = false;
            _pageDeployLblSuccess.visible = false;
        }

        private void UpdateSceneList()
        {
            _pageDeployLstScenes.ClearSelection();
            _pageDeployLstScenes.itemsSource = null;

            string[] allSceneGuids = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" });
            List<SceneAsset> allScenes = new();

            foreach (string guid in allSceneGuids)
            {
                allScenes.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            _pageDeployLstScenes.makeItem = () => new Label();
            _pageDeployLstScenes.bindItem = (item, index) => { (item as Label).text = allScenes[index].name; };
            _pageDeployLstScenes.itemsSource = allScenes;

            SetButtonEnabled(_pageDeployBtnExport, false);
            SetButtonEnabled(_pageDeployBtnRunLocal, false);
        }

        private void SceneSelected(IEnumerable<object> scenes)
        {
            if (!scenes.Any()) { return; }

            SceneAsset scene = scenes.First() as SceneAsset;
            if (scene == null)
            {
                return;
            }

            _sceneToExport = scene;

            SetButtonEnabled(_pageDeployBtnExport, true);
            SetButtonEnabled(_pageDeployBtnRunLocal, true);
        }

        private bool ExportScene()
        {
#if !IMMERZA_SDK_INSTALLED
            Log.LogError("ImmerzaSDK not installed. Please install the SDK before proceeding", LogChannelType.SDKManager);
            return false;
#else
            Log.LogInfo("Export scene...", LogChannelType.SDKManager);

            ISceneBuilder sceneBuilder = SDKAPIBridge.SceneBuilder;

            if (sceneBuilder == null)
            {
                Log.LogError("scene builder was not initialized by SDK", LogChannelType.SDKManager);
                return false;
            }

            ResetDeployView();

            ExportSettings exportSettings = new ExportSettings
            {
                SceneToExport = _sceneToExport,
                ExportFolder = _pageDeployTxtPath.text
            };

            bool buildSuccesful = false;

            if (sceneBuilder.PrepareForExport(exportSettings))
            {
                if (!PreflightCheckManager.RunChecks())
                {
                    Log.LogInfo("...canceled as for runtime checks not passing", LogChannelType.SDKManager);
                    SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, false, "Some preflight checks failed. Please check status page for more details", "<u>View Status-Page</u>", () => {
                        _pageDeployParenTabView.selectedTabIndex = TAB_INDEX_STATUS;
                     });
                    _pageDeployLblAction.visible = true;
                }
                else
                {
                    if (!SDKAPIBridge.SceneBuilder.ExportScene(exportSettings))
                    {
                        SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, false, "Export failed, please contact support@immerza.de");
                    }
                    else
                    {
                        buildSuccesful = true;

                        SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, true, "Experience exported successfully");

                        if (_pageDeployTglOpenExportFolder.value)
                        {
                            Process.Start("explorer.exe", exportSettings.ExportFolder);
                        }
                    }
                }
            }
            else
            {
                Log.LogInfo("...prepare for export failed", LogChannelType.SDKManager);
            }

            return buildSuccesful;
#endif
        }

        private void ExportAndRunScene()
        {
            string simulatorPath = string.Empty;
            if (!SimulatorInstalled(ref simulatorPath))
            {
                Log.LogError($"Simulator was not found under in '{simulatorPath}'", LogChannelType.SDKManager);
                SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, false, "Simulator was not found. Please install the simulator in <project_directory>\\ImmerzaSimulator");
                return;
            }

            bool validBuildFound = false;
            if (CheckForValidBuild(_pageDeployTxtPath.text))
            {
                if (!EditorUtility.DisplayDialog("Run Build", "A build was found in the export folder. Do you want to run this build without exporting again?", "Yes", "No"))
                {
                    ExportScene();
                }

                validBuildFound = CheckForValidBuild(_pageDeployTxtPath.text);
            }

            if (validBuildFound)
            {
                string arguments = $"--load-on-start \"{_pageDeployTxtPath.text}\"";
                Process.Start(simulatorPath, arguments);
            }
            else
            {
                Log.LogError($"Failed to run local build, no valid build found in folder {_pageDeployTxtPath.text}", LogChannelType.SDKManager);
            }
        }
        private static async Task<List<JToken>> LoadSceneData(string sceneName, AuthData authData)
        {
            string route = Constants.API_ROUTE_ACTIVITY_DEFINITION +
                           $"?name:exact={sceneName}" +
                           $"&publisher={authData.User.ResourceType}/{authData.User.Id}";

            var responseText = await Client.Get(route, authData);
            List<JToken> result = new();
            try
            {
                JObject response = JObject.Parse(responseText);
                JToken matches = response.GetValue("entry");
                if (matches != null)
                {
                    foreach (JToken item in matches)
                    {
                        result.Add(item["resource"]);
                    }
                }
            }
            catch (JsonReaderException)
            {
            }

            return result;
        }

        private static async Task<string> UploadFile(string exportPath, string filename, string contentMimeType, AuthData authData)
        {
            string responseText = await Client.Post(Constants.API_ROUTE_FILES, new Dictionary<string, string> {
                { "contentType", contentMimeType },
                { "fileName", Path.GetFileName(filename) }
            }, authData);

            if (string.IsNullOrEmpty(responseText))
            {
                return string.Empty;
            }

            JObject response = JObject.Parse(responseText);

            string bucketUrl = response["uploadOptions"]["url"].Value<string>();
            WWWForm data = new();
            foreach (JToken item in response["uploadOptions"]["fields"])
            {
                JProperty property = item as JProperty;
                if (property != null)
                    data.AddField(property.Name, property.Value.ToString());
            }

            data.AddBinaryData("file", File.ReadAllBytes(Path.Combine(exportPath, filename)));

            string fileId = string.Empty;
            using (UnityWebRequest uploadRequest = UnityWebRequest.Post(bucketUrl, data))
            {
                await uploadRequest.SendWebRequest();
                if (uploadRequest.result == UnityWebRequest.Result.Success)
                {
                    fileId = response["id"].Value<string>();
                }
            }

            return fileId;
        }

        private static async Task<string> UploadBundles(string exportPath, AuthData authData)
        {
            const int WindowsBundleTaskIndex = 0;
            const int AndroidBundleTaskIndex = 1;

            Task<string>[] uploadTasks = new Task<string>[2];
            uploadTasks[WindowsBundleTaskIndex] = UploadFile(exportPath, BUILD_ARTIFACT_BUNDLE_WIN, string.Empty, authData);
            uploadTasks[AndroidBundleTaskIndex] = UploadFile(exportPath, BUILD_ARTIFACT_BUNDLE_ANDROID, string.Empty, authData);

            string metadata = File.ReadAllText(Path.Combine(exportPath, BUILD_ARTIFACT_METADATA));
            JObject catalog = JObject.Parse(metadata);
            bool uploadSuccessful = true;
            foreach (JToken platform in catalog["platforms"])
            {
                string platformId = platform["platform_id"].Value<string>();
                string url = string.Empty;
                if (platformId == "win")
                {
                    url = await uploadTasks[WindowsBundleTaskIndex];
                }
                else if (platformId == "android")
                {
                    url = await uploadTasks[AndroidBundleTaskIndex];
                }
                else
                {
                    Log.LogWarning($"found unknown platform id in scene metadata {platformId}", LogChannelType.SDKManager);
                }

                if (!string.IsNullOrEmpty(url))
                {
                    (platform["file_id"].Parent as JProperty).Value = url;
                }
                else
                {
                    Log.LogError($"upload failed for {platformId}", LogChannelType.SDKManager);
                    uploadSuccessful = false;
                    break;
                }
            }

            if (uploadSuccessful)
            {
                File.WriteAllText(Path.Combine(exportPath, BUILD_ARTIFACT_METADATA), catalog.ToString());
                return await UploadFile(exportPath, BUILD_ARTIFACT_METADATA, "application/json", authData);
            }

            return string.Empty;
        }

        private async void UploadScene()
        {
            if (!await Auth.CheckAuthData(_authData))
            {
                Log.LogError("Refreshing access token failed...", LogChannelType.SDKManager);
                return;
            }

            bool validBuildFound = false;
            if (CheckForValidBuild(_pageDeployTxtPath.text))
            {
                if (!EditorUtility.DisplayDialog("Run Build", "A build was found in the export folder. Do you want to upload this build without exporting again?", "Yes", "No"))
                {
                    ExportScene();
                }

                validBuildFound = CheckForValidBuild(_pageDeployTxtPath.text);
            }

            if (!validBuildFound)
            {
                Log.LogError($"Failed to upload scene, no valid build found in folder {_pageDeployTxtPath.text}", LogChannelType.SDKManager);
                SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, false, "The specified build folder doesn't contain a valid build");
                return;
            }

            List<JToken> sceneDataList = null;
             
            if (!string.IsNullOrEmpty(_pageDeployTxtExperienceName.text))
            {
                sceneDataList = await LoadSceneData(_pageDeployTxtExperienceName.text, _authData);
            }
            if (sceneDataList == null || sceneDataList.Count == 0)
            {
                Log.LogError($"Failed to upload scene, no name for experience specified", LogChannelType.SDKManager);
                SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, false, "Please specify a valid name for the experience to upload to. If no experience exists, make sure to create an experience on the contributor page.", "<u>https://yourworld.immerza.com/</u>", () => {
                    Process.Start("explorer", "https://yourworld.immerza.com/");
                });
                return;
            }
            else if (sceneDataList.Count > 1)
            {
                Log.LogError($"Failed to upload scene, multiple scenes with the same name where found", LogChannelType.SDKManager);
                SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, false, "Multiple experiences with the same name where found. Please check the experiences with the contributor page.", "<u>https://yourworld.immerza.com/</u>", () => {
                    Process.Start("explorer", "https://yourworld.immerza.com/");
                });
                return;
            }

            string newCatalogUrl = await UploadBundles(_pageDeployTxtPath.text, _authData);

            string catalogUpdateResponse = string.Empty;
            if (!string.IsNullOrEmpty(newCatalogUrl))
            {
                JToken sceneData = sceneDataList[0];
                foreach (JToken artifact in sceneData["relatedArtifact"])
                {
                    if (artifact["type"].Value<string>() == "json")
                    {
                        JProperty url = artifact["url"].Parent as JProperty;
                        url.Value = newCatalogUrl;
                        break;
                    }
                }

                string route = Constants.API_ROUTE_ACTIVITY_DEFINITION + $"/{sceneData["id"]}";
                catalogUpdateResponse = await Client.Put(route, sceneData.ToString(), _authData);
            }

            if (string.IsNullOrEmpty(catalogUpdateResponse))
            {
                Log.LogError($"Failed to upload scene, updating scene definition failed", LogChannelType.SDKManager);
                SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, false, "Failed to upload the scene");
            }
            else
            {
                SetLabelMsg(_pageDeployLblSuccess, _pageDeployLblAction, true, "Scene export uploaded successfully");
            }
        }

        private static void SetButtonEnabled(Button button, bool enabled)
        {
            button.SetEnabled(enabled);
            if (enabled)
            {
                button.style.backgroundColor = new UnityEngine.Color(0.4f, 0.4f, 0.4f);
                button.style.color = new UnityEngine.Color(1.0f, 1.0f, 1.0f);
            }
            else
            {
                button.style.backgroundColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f);
                button.style.color = new UnityEngine.Color(0.3f, 0.3f, 0.3f);
            }
        }

        private static bool CheckForValidBuild(string buildPath)
        {
            foreach (string artifact in new string[]{ BUILD_ARTIFACT_BUNDLE_WIN, BUILD_ARTIFACT_BUNDLE_ANDROID, BUILD_ARTIFACT_METADATA })
            {
                if (!File.Exists(Path.Combine(buildPath, artifact)))
                    return false;
            }
            return true;
        }

        private static bool SimulatorInstalled(ref string path)
        {
            path = Path.Combine(Application.dataPath, "../ImmerzaSimulator/Immerza.exe");
            if (File.Exists(path))
            {
                return true;
            }
            return false;
        }
    }
}
