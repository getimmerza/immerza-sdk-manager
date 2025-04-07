using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace ImmerzaSDK.Manager.Editor
{
    public partial class SDKManagerWindow : EditorWindow
    {
        private const int TAB_INDEX_STATUS = 0;

        [SerializeField]
        private VisualTreeAsset _treeAssetMainPage = null;
        [SerializeField]
        private VisualTreeAsset _treeAssetAuthPage = null;
        [SerializeField]
        private Sprite _logo = null;

        #region UI Elements
        private Button _mainPageBtnOpenLog;
        private Button _mainPageBtnPreflightChecks;
        #endregion

        [MenuItem("Immerza/SDK Manager")]
        public static void ShowWindow()
        {
            GetWindow<SDKManagerWindow>("Immerza SDK Manager");
        }

        private void clearVisualRoot()
        {
            rootVisualElement.Clear();
        }

        private async Awaitable initializeMainPage()
        {
            clearVisualRoot();

            _treeAssetMainPage.CloneTree(rootVisualElement);
            TabView mainPageRoot = rootVisualElement.Q<TabView>("MainPage");

            _mainPageBtnOpenLog = rootVisualElement.Q<Button>("OpenLogButton");
            _mainPageBtnOpenLog.clicked += _mainPageBtnOpenLog_onClick;
            _mainPageBtnPreflightChecks = rootVisualElement.Q<Button>("PreflightChecksButton");
            _mainPageBtnPreflightChecks.clicked += _mainPageBtnPreflightChecks_clicked;

            List <Task> initializationTasks = new()
            {
                InitializeUpdateView(mainPageRoot.Q<VisualElement>("UpdatePage"))
            };
            InitializeStatusView(mainPageRoot.Q<VisualElement>("StatusPage"));
            InitializeAccountView(mainPageRoot.Q<VisualElement>("AccountPage"));
            InitializeDeployView(mainPageRoot, mainPageRoot.Q<VisualElement>("DeployPage"));
            await Task.WhenAll(initializationTasks);
        }

        private void _mainPageBtnPreflightChecks_clicked()
        {
            PreflightCheckManager.RunChecks();
        }

        private void _mainPageBtnOpenLog_onClick()
        {
            Process.Start("explorer.exe", Path.GetFullPath(Application.persistentDataPath));
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

        private static void SetButton(Button button, bool activate)
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

        private static void SetLabelMsg(Label label, bool success, string message)
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
    }
}
