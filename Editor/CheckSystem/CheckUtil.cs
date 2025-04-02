using ImmerzaSDK.Lua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XLua;

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

        public static HashSet<string> GetAllLuaBindingNames()
        {
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            HashSet<string> allLuaBindings = new();

            List<Type> luaBindingClasses = new() {
                typeof(GenConfig),
                typeof(XRGenConfig)
            };

            foreach (var assembly in allAssemblies)
            {
                Type[] types = assembly.GetTypes();

                foreach (var type in types)
                {
                    if (type.GetCustomAttributes(typeof(LuaCallCSharpAttribute), false).Any())
                    {
                        allLuaBindings.Add($"CS.{type.FullName}");
                    }
                }
            }

            foreach (Type genConfig in luaBindingClasses)
            {
                FieldInfo field = genConfig.GetField("LuaCallCSharp", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                {
                    object value = field.GetValue(null);
                    if (value is List<Type> typeList)
                    {
                        foreach (Type type in typeList)
                        {
                            allLuaBindings.Add($"CS.{type.FullName}");
                        }
                    }
                }
            }

            return allLuaBindings;
        }
    }
}
