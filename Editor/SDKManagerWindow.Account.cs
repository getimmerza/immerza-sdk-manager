using UnityEngine.UIElements;

namespace ImmerzaSDK.Manager.Editor
{
    public partial class SDKManagerWindow
    {
        #region UI Elements
        private Label _pageAccountLblUserName;
        private Label _pageAccountLblUserMail;
        private Button _pageAccountBtnLogout;
        #endregion

        private void InitializeAccountView(VisualElement pageRoot)
        {
            _pageAccountLblUserName = pageRoot.Q<Label>("UserNameLabel");
            _pageAccountLblUserName.text = _authData.User.Name;
            _pageAccountLblUserMail = pageRoot.Q<Label>("UserMailLabel");
            _pageAccountLblUserMail.text = _authData.User.Mail;

            _pageAccountBtnLogout = pageRoot.Q<Button>("LogoutButton");
            _pageAccountBtnLogout.clicked += Logout;
        }

        private void Logout()
        {
            Auth.ClearLogoutData();
            _authData = Auth.InvalidAuthData;
            initializeAuthPage();
        }
    }
}
