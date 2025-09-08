using UnityEditor;
using UnityEngine;


namespace ImmerzaSDK.Manager.Editor
{
    public static class SDKManagerCLI
    {
        public static void ExportScene()
        {
            string outputPath = "";
            string sceneName = "";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-exportPath" && i + 1 < args.Length)
                {
                    outputPath = args[i + 1];
                }
                else if (args[i] == "-sceneName" && i+1 < args.Length)
                {
                    sceneName = args[i + 1];
                }
            }

            if (string.IsNullOrEmpty(outputPath) || string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("Scene not found or export path not set!");
                return; 
            }

            SceneAsset sceneAsset = null;
            
            string[] guids = AssetDatabase.FindAssets("t:Scene " + sceneName);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                if (fileName == sceneName)
                {
                    sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                }
            }

            if (sceneAsset == null)
            {
                Debug.LogError("Scene not found!");
                return;
            }
            
            ExportSettings exportSettings = new ExportSettings
            {
                ExportFolder = outputPath,
                SceneToExport = sceneAsset
            };
            
            SDKAPIBridge.SceneBuilder.PrepareForExport(exportSettings);
            SDKAPIBridge.SceneBuilder.ExportScene(exportSettings);
        }
        
        
        private static void UpdateSDK()
        {
            
        }
    }
}
