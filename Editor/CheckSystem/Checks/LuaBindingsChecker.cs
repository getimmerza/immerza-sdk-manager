using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class LuaBindingsChecker : ICheckable
{
    List<CheckResult> ICheckable.RunCheck()
    {
        HashSet<string> luaBindings = CheckUtil.GetAllLuaBindingNames();
        List<LuaAsset> luaScripts = CheckUtil.GetLuaAssets();
        List<CheckResult> illegalBindings = new();

        Regex typeRegex = new(@"\bCS\.[A-Za-z_][A-Za-z0-9_.]*\b", RegexOptions.Compiled);

        foreach (LuaAsset script in luaScripts)
        {
            string content = script.content;

            foreach (Match match in typeRegex.Matches(content))
            {
                string foundType = match.Value;

                if (!luaBindings.Contains(foundType))
                {
                    illegalBindings.Add(new CheckResult()
                    {
                        Type = ResultType.Warning,
                        Message = $"Unknown type found in {script.name}: {foundType}",
                        ContextObject = script
                    });
                }
            }
        }

        if (illegalBindings.Count == 0)
        {
            return new List<CheckResult>
            {
                new()
                {
                    Type = ResultType.Success,
                    Message = "LuaBindingsChecker succeeded.",
                    ContextObject = null
                }
            };
        }
        else
        {
            return illegalBindings;
        }
    }
}
