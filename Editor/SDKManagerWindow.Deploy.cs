    using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ImmerzaSDK.Manager.Editor
{
    public partial class SDKManagerWindow
    {
        #region UI Elements
        private ListView _pageDeployLstScenes;
        private TextField _pageDeployTxtPath;
        private Button _pageDeployBtnExport;
        private Button _pageDeployBtnRunLocal;
        private Button _pageDeployBtnUpload;
        private Button _pageDeployBtnRefresh;
        private Toggle _pageDeployTglOpenExportFolder;
        private Label _pageDeployLblSuccess;
        private Label _pageDeployLblAction;
        #endregion

        private SceneAsset _sceneToExport;

        private void InitializeDeployView(TabView parentTabView, VisualElement pageRoot)
        {
            _pageDeployLstScenes = pageRoot.Q<ListView>("SceneList");
            _pageDeployLstScenes.selectionChanged += SceneSelected;

            _pageDeployTxtPath = pageRoot.Q<TextField>("ExportPath");

            _pageDeployBtnExport = pageRoot.Q<Button>("ExportButton");
            _pageDeployBtnExport.clicked += () => ExportScene();

            _pageDeployBtnRunLocal = pageRoot.Q<Button>("RunLocalButton");
            _pageDeployBtnRunLocal.clicked += ExportAndRunScene;

            _pageDeployBtnUpload = pageRoot.Q<Button>("UploadButton");
            _pageDeployBtnUpload.clicked += UploadScene;

            _pageDeployBtnRefresh = pageRoot.Q<Button>("RefreshButton");
            _pageDeployBtnRefresh.clicked += UpdateSceneList;

            _pageDeployTglOpenExportFolder = pageRoot.Q<Toggle>("OpenExportFolder");

            _pageDeployLblSuccess = pageRoot.Q<Label>("SuccessLabel");

            _pageDeployLblAction = pageRoot.Q<Label>("ActionLink");
            _pageDeployLblAction.AddManipulator(new Clickable(x =>
                parentTabView.selectedTabIndex = TAB_INDEX_STATUS
             ));

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
                    SetLabelMsg(_pageDeployLblSuccess, false, "Some preflight checks failed. Please check status page for more details");
                    _pageDeployLblAction.visible = true;
                }
                else
                {
                    if (!SDKAPIBridge.SceneBuilder.ExportScene(exportSettings))
                    {
                        SetLabelMsg(_pageDeployLblSuccess, false, "Export failed, please contact support@immerza.de");
                    }
                    else
                    {
                        buildSuccesful = true;

                        SetLabelMsg(_pageDeployLblSuccess, true, "Experience exported successfully");

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
                SetLabelMsg(_pageDeployLblSuccess, false, "Simulator was not found. Please install the simulator in <project_directory>\\ImmerzaSimulator");
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
        private void UploadScene()
        {
            if (!CheckForValidBuild(_pageDeployTxtPath.text))
            {
                Log.LogError($"Failed to upload scene, no valid build found in folder {_pageDeployTxtPath.text}", LogChannelType.SDKManager);
                SetLabelMsg(_pageDeployLblSuccess, false, "The specified build folder doesn't contain a valid build");
                return;
            }

            SetLabelMsg(_pageDeployLblSuccess, false, "Not implemented yet");
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
            string[] buildArtifacts =
            {
                "immerza_metadata.json",
                "immerza_scene_win.bundle",
                "immerza_scene_android.bundle"
            };

            foreach (string artifact in buildArtifacts)
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
