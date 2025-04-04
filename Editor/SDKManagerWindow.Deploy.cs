﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace ImmerzaSDK.Manager.Editor
{
    public partial class SDKManagerWindow
    {
        #region UI Elements
        private ListView _pageDeployLstScenes;
        private TextField _pageDeployTxtPath;
        private Button _pageDeployBtnExport;
        private Button _pageDeployBtnRefresh;
        private Toggle _pageDeployTglOpenExportFolder;
        private Label _pageDeployLblSuccess;
        #endregion

        private SceneAsset _sceneToExport;

        private void InitializeDeployView(VisualElement pageRoot)
        {
            _pageDeployLstScenes = pageRoot.Q<ListView>("SceneList");
            _pageDeployLstScenes.selectionChanged += SceneSelected;

            _pageDeployTxtPath = pageRoot.Q<TextField>("ExportPath");

            _pageDeployBtnExport = pageRoot.Q<Button>("ExportButton");
            _pageDeployBtnExport.SetEnabled(false);
            _pageDeployBtnExport.style.backgroundColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f);
            _pageDeployBtnExport.style.color = new UnityEngine.Color(0.3f, 0.3f, 0.3f);
            _pageDeployBtnExport.clicked += ExportScene;

            _pageDeployBtnRefresh = pageRoot.Q<Button>("RefreshButton");
            _pageDeployBtnRefresh.clicked += UpdateSceneList;

            _pageDeployTglOpenExportFolder = pageRoot.Q<Toggle>("OpenExportFolder");

            _pageDeployLblSuccess = pageRoot.Q<Label>("SuccessLabel");
            _pageDeployLblSuccess.visible = false;

            UpdateSceneList();
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

            _pageDeployBtnExport.SetEnabled(false);
            _pageDeployBtnExport.style.backgroundColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f);
            _pageDeployBtnExport.style.color = new UnityEngine.Color(0.3f, 0.3f, 0.3f);
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

            _pageDeployBtnExport.SetEnabled(true);
            _pageDeployBtnExport.style.backgroundColor = new UnityEngine.Color(0.4f, 0.4f, 0.4f);
            _pageDeployBtnExport.style.color = new UnityEngine.Color(1.0f, 1.0f, 1.0f);
        }

        private void ExportScene()
        {
            if (SDKAPIBridge.SceneBuilder == null)
                return;

            ExportSettings exportSettings = new ExportSettings
            {
                SceneToExport = _sceneToExport,
                ExportFolder = _pageDeployTxtPath.text
            };

            if (!SDKAPIBridge.SceneBuilder.ExportScene(exportSettings))
            {
                SetLabelMsg(_pageDeployLblSuccess, false, "Export failed, please contact support@immerza.de");
            }
            else
            {
                SetLabelMsg(_pageDeployLblSuccess, true, "Experience exported successfully");

                if (_pageDeployTglOpenExportFolder.value)
                {
                    Process.Start("explorer.exe", exportSettings.ExportFolder);
                }
            }
        }
    }
}
