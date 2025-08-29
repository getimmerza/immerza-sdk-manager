using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CheckableAttribute(displayName: "LUA OnPauseRequested Check")]
public class OnPauseRequestedChecker : ICheckable
{
    public void RunCheck(CheckContext context)
    {
        List<LuaAsset> scripts = CheckUtil.GetLuaAssets();

        Regex regex = new("OnPauseRequested\\('\\+'");

        foreach (LuaAsset script in scripts)
        {
            if (regex.IsMatch(script.content))
            {
                return;
            }
        }

        context.AddError("None of the Lua scripts implement OnPauseRequested, which is required before exporting.");
    }
}
