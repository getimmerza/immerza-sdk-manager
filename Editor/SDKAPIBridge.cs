using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    [InitializeOnLoad]
    public static class SDKAPIBridge
    {
        private const string SessionIdPreferenceKey = "SessionId";

        public static ISceneBuilder SceneBuilder { get; private set; }

        public static bool DomainReload { get; private set; }

        static SDKAPIBridge()
        {
            AssemblyReloadEvents.beforeAssemblyReload += HandleAssembleBeforeReload;
            AssemblyReloadEvents.afterAssemblyReload += HandleAssembleAfterReload;
            EditorApplication.quitting += EditorApplication_quitting;

            // domain was reloaded but editor didn't start new
            if (EditorPrefs.HasKey(SessionIdPreferenceKey))
            {
                DomainReload = true;
            }
            else
            {
                EditorPrefs.SetString(SessionIdPreferenceKey, System.Guid.NewGuid().ToString());
                DomainReload = false;
            }
        }

        private static void EditorApplication_quitting()
        {
            EditorPrefs.DeleteKey(SessionIdPreferenceKey);
        }

        public static void RegisterSceneBuilder(ISceneBuilder sceneBuilder)
        {
            SceneBuilder = sceneBuilder;
        }
 
        private static void HandleAssembleBeforeReload()
        {
            Log.FlushAndTeardown();
        }

        private static void HandleAssembleAfterReload()
        {
            Log.RegisterChannel(new LogChannelFile("EditorLog", LogChannelType.SDKManager | LogChannelType.SDK));
            Log.RegisterChannel(new LogChannelUnity());
        }
    }
}