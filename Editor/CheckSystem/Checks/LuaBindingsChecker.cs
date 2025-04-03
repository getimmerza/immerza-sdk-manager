using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[CheckableAttribute(displayName: "Lua Bindings Check")]
public class LuaBindingsChecker : ICheckable
{
    public void RunCheck(CheckContext context)
    {
        HashSet<string> luaBindings = CheckUtil.GetAllLuaBindingNames();
        List<LuaAsset> luaScripts = CheckUtil.GetLuaAssets();
        
        Regex typeRegex = new(@"\bCS\.[A-Za-z_][A-Za-z0-9_.]*\b", RegexOptions.Compiled);

        foreach (LuaAsset script in luaScripts)
        {
            string content = script.content;

            foreach (Match match in typeRegex.Matches(content))
            {
                string foundType = match.Value;

                if (!luaBindings.Contains(foundType))
                {
                    context.AddWarning($"Unknown type found in {script.name}: {foundType}", script);
                }
            }
        }
    }
}
