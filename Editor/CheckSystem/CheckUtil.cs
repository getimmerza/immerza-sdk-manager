using ImmerzaSDK.Lua;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    public static class CheckUtil
    {
        public static List<LuaAsset> GetLuaAssets()
        {
            string[] scriptGUIDs = AssetDatabase.FindAssets("t: LuaAsset");

            List<LuaAsset> scripts = new();

            foreach (string guid in scriptGUIDs)
            {
                scripts.Add(AssetDatabase.LoadAssetAtPath<LuaAsset>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            return scripts;
        }
    }
}
