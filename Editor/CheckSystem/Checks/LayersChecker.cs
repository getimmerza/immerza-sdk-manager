using ImmerzaSDK.Manager.Editor;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using UnityEditor;
using UnityEngine;

[CheckableAttribute(displayName: "Validate Layers Check")]
public class LayersChecker : ICheckable
{
    private const int NumReservedLayers = 16;
    private readonly Dictionary<int, string> PrivateLayers = new()
    {
        { 0, "Default" },
        { 1, "TransparentFX" },
        { 2, "Ignore Raycast" },
        { 4, "Water" },
        { 5, "UI" }
    };

    public void RunCheck(CheckContext context   )
    {
        SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty serializedProperty = serializedObject.FindProperty("layers");
        
        string name;
        for (int i = 0; i < NumReservedLayers; ++i)
        {
            name = serializedProperty.GetArrayElementAtIndex(i).stringValue;
            if (name != PrivateLayers.GetValueOrDefault(i, string.Empty))
            {
                context.AddError($"Layer {i} ({name}) is a private layer and cannot be used by the scenario");
            }
        }

        name = serializedProperty.GetArrayElementAtIndex(31).stringValue;
        if (!string.IsNullOrEmpty(name))
        {
            context.AddError("Layer 31 cannot be used as it is used within Unity Editor for special purpose");
        }
    }
}
