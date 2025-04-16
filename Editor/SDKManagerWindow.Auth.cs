using UnityEngine;
using UnityEngine.UIElements;

namespace ImmerzaSDK.Manager.Editor
{
    public partial class SDKManagerWindow
    {
        #region UI Elements
        private TextField _pageAuthTxtEmail;
        private TextField _pageAuthTxtPassword;
        private Label _pageAuthLblMessage;
        private Button _pageAuthBtnSignIn;
        #endregion

        private AuthData _authData;

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

            _pageAuthTxtEmail = authRoot.Q<TextField>("EmailField");
            _pageAuthTxtPassword = authRoot.Q<TextField>("PasswordField");
            _pageAuthLblMessage = authRoot.Q<Label>("AuthMessage");
            _pageAuthBtnSignIn = authRoot.Q<Button>("SignInButton");

            _pageAuthBtnSignIn.clicked += HandleSignIn;

            EventCallback<KeyDownEvent> onKeyDown = (KeyDownEvent ev) =>
            {
                if (ev.keyCode == KeyCode.Return)
                {
                    HandleSignIn();
                }
            };
            _pageAuthTxtEmail.RegisterCallback<KeyDownEvent>(onKeyDown, TrickleDown.TrickleDown);
            _pageAuthTxtPassword.RegisterCallback<KeyDownEvent>(onKeyDown, TrickleDown.TrickleDown);
        }

        private async void HandleSignIn()
        {
            Log.LogInfo("Sign in to contributer platform", LogChannelType.SDKManager);

            if (string.IsNullOrEmpty(_pageAuthTxtEmail.text) || string.IsNullOrEmpty(_pageAuthTxtPassword.text))
            {
                Log.LogInfo("... failed due to missing credentials", LogChannelType.SDKManager);
                SetLabelMsg(_pageAuthLblMessage, null, false, "Please provide both the email address and password to your Immerza account.");
                return;
            }

            _pageAuthBtnSignIn.SetEnabled(false);
            _pageAuthLblMessage.visible = false;

            string signInMsg;
            (_authData, signInMsg) = await Auth.SignIn(_pageAuthTxtEmail.text, _pageAuthTxtPassword.text);

            if (_authData == null)
            {
                Log.LogError($"Sign in failed: {signInMsg}", LogChannelType.SDKManager);
                SetLabelMsg(_pageAuthLblMessage, null, false, signInMsg);
                _pageAuthBtnSignIn.SetEnabled(true);
                return;
            }

            await initializeMainPage();
        }
    }
}
