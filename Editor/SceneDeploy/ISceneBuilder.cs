using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    public struct ExportSettings
    {
        public SceneAsset SceneToExport;
        public string ExportFolder;
    }

    public interface ISceneBuilder
    {
        public bool PrepareForExport(ExportSettings exportSettings);
        public bool ExportScene(ExportSettings exportSettings);
    }
}
