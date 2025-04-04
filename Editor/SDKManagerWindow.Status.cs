using System;
using UnityEngine.UIElements;

namespace ImmerzaSDK.Manager.Editor
{
    public partial class SDKManagerWindow
    {
        #region UI Elements
        private GroupBox _pageStatusGrpError;
        private GroupBox _pageStatusGrpWarning;
        private Label _pageStatusLblWarningCount;
        private Label _pageStatusLblErrorCount;
        #endregion

        private int _errorCount = 0;
        private int _warningCount = 0;

        private void InitializeStatusView(VisualElement pageRoot)
        {
            _pageStatusGrpError = pageRoot.Q<GroupBox>("ErrorBox");
            _pageStatusGrpWarning = pageRoot.Q<GroupBox>("WarningsBox");
            _pageStatusLblWarningCount = pageRoot.Q<Label>("WarningsCount");
            _pageStatusLblErrorCount = pageRoot.Q<Label>("ErrorsCount");

#if IMMERZA_SDK_INSTALLED
            PreflightCheckManager.OnLogCheck += HandleNewCheckResults;
            PreflightCheckManager.OnBeforeRunChecks += OnBeforeRunChecks;
            DispatchChecks();
#endif
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
                _pageStatusLblErrorCount.text = Convert.ToString(++_errorCount);
                Label newMsg = new(message);
                newMsg.AddToClassList("label-wrap");
                _pageStatusGrpError.Add(newMsg);
            }
            else if (type == ResultType.Warning)
            {
                _pageStatusLblWarningCount.text = Convert.ToString(++_warningCount);
                _pageStatusGrpWarning.Add(new Label(message));
            }
        }

        private void OnBeforeRunChecks()
        {
            _warningCount = 0;
            _errorCount = 0;
            _pageStatusGrpError.Clear();
            _pageStatusGrpWarning.Clear();
            _pageStatusLblWarningCount.text = "0";
            _pageStatusLblErrorCount.text = "0";
        }
#endif
    }
}
