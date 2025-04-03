using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    public static class SDKAPIBridge
    {
        public static ISceneBuilder SceneBuilder { get; private set; }

        public static void RegisterSceneBuilder(ISceneBuilder sceneBuilder)
        {
            SceneBuilder = sceneBuilder;
        }
    }
}