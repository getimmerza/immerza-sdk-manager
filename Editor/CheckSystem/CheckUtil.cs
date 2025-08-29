using ImmerzaSDK.Lua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using XLua;

namespace ImmerzaSDK.Manager.Editor
{
    public static class CheckUtil
    {
        private static readonly Regex NamespaceRegex = new(@"^(CS\.[\w\.]+)\s*=\s*\1\s+or\s+\{\}", RegexOptions.Compiled);
        private static readonly Regex ClassRegex = new(@"CS\.[\w\.]+", RegexOptions.Compiled);
        private static readonly Regex FunctionRegex = new(@"function\s+(CS\.[\w\.]+)", RegexOptions.Compiled);
        private static readonly Regex ShorthandFunctionRegex = new(@"function\s+[\w_]+[:\.]([\w_]+)", RegexOptions.Compiled);
        private static readonly Regex FieldRegex = new(@"---@field\s+([\w_]+)", RegexOptions.Compiled);

        public static List<LuaAsset> GetLuaAssets()
        {
            string[] scriptGUIDs = AssetDatabase.FindAssets("t: LuaAsset", new[] { "Assets/Scripts" });

            List<LuaAsset> scripts = new();

            foreach (string guid in scriptGUIDs)
            {
                scripts.Add(AssetDatabase.LoadAssetAtPath<LuaAsset>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            return scripts;
        }

        public static HashSet<string> GetAllLuaBindingNames()
        {
            HashSet<string> allBindings = new();
            string stubPath = Path.Combine("Assets", "Immerza", "LuaAutocompletion");

            if (!Directory.Exists(stubPath))
            {
                return allBindings;
            }
            
            string[] stubFiles = Directory.GetFiles(stubPath, "*.lua", SearchOption.AllDirectories);

            string currentClass = null;

            foreach (string file in stubFiles)
            {
                string[] lines = File.ReadAllLines(file);

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    // match namespace
                    Match orAssignMatch = NamespaceRegex.Match(trimmed);
                    if (orAssignMatch.Success)
                    {
                        string fullName = orAssignMatch.Groups[1].Value;
                        allBindings.Add(fullName);
                        currentClass = fullName;
                        continue;
                    }

                    // match class
                    if (trimmed.StartsWith("---@class CS."))
                    {
                        Match match = ClassRegex.Match(trimmed);
                        if (match.Success)
                        {
                            currentClass = match.Value;
                            allBindings.Add(currentClass);
                            continue;
                        }
                    }

                    // match function
                    if (currentClass != null && trimmed.StartsWith("function"))
                    {
                        Match funcMatch = FunctionRegex.Match(trimmed);
                        if (funcMatch.Success)
                        {
                            allBindings.Add(funcMatch.Groups[1].Value);
                        }
                        else
                        {
                            Match shorthandMatch = ShorthandFunctionRegex.Match(trimmed);
                            if (shorthandMatch.Success)
                            {
                                string method = shorthandMatch.Groups[1].Value;
                                allBindings.Add($"{currentClass}.{method}");
                            }
                        }
                    }

                    // match field
                    if (currentClass != null && trimmed.StartsWith("---@field"))
                    {
                        Match fieldMatch = FieldRegex.Match(trimmed);
                        if (fieldMatch.Success)
                        {
                            string field = fieldMatch.Groups[1].Value;
                            allBindings.Add($"{currentClass}.{field}");
                        }
                    }
                }
            }

            return allBindings;
        }
    }
}
