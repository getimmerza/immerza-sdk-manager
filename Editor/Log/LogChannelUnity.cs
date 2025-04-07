using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImmerzaSDK.Manager.Editor
{
    internal class LogChannelUnity : ILogChannel
    {
        public LogChannelFlags Flags => LogChannelFlags.JustRawMessag;

        public LogChannelType Type => LogChannelType.SDKManager;

        public void Dispatch(LogInfo logInfo)
        {
            switch (logInfo.Severity)
            {
                case LogSeverity.Error:
                    UnityEngine.Debug.LogError(logInfo.Message);
                    break;
                case LogSeverity.Warning:
                    UnityEngine.Debug.LogWarning(logInfo.Message);
                    break;
                case LogSeverity.Info:
                    UnityEngine.Debug.Log(logInfo.Message);
                    break;
            }
        }
    }
}
