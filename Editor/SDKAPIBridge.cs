using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    [InitializeOnLoad]
    public static class SDKAPIBridge
    {
        public static ISceneBuilder SceneBuilder { get; private set; }

        static SDKAPIBridge()
        {
            AssemblyReloadEvents.beforeAssemblyReload += HandleAssembleBeforeReload;
            AssemblyReloadEvents.afterAssemblyReload += HandleAssembleAfterReload;
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
            Log.RegisterChannel(new LogChannelFile("EditorLog", LogChannelType.EditorSDK));
            Log.RegisterChannel(new LogChannelUnity());
        }
    }
}