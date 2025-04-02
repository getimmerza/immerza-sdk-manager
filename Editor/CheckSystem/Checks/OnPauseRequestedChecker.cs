using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class OnPauseRequestedChecker : ICheckable
{
    List<CheckResult> ICheckable.RunCheck()
    {
        List<LuaAsset> scripts = CheckUtil.GetLuaAssets();

        Regex regex = new("OnPauseRequested\\('\\+'");

        foreach (LuaAsset script in scripts)
        {
            if (regex.IsMatch(script.content))
            {
                return new List<CheckResult> {
                    new()
                    {
                        Type = ResultType.Success,
                        Message = "OnPauseRequested check succeeded",
                        ContextObject = null
                    }
                };
            }
        }

        return new List<CheckResult> {
            new()
            {
                Type = ResultType.Error,
                Message = "None of the Lua scripts implement OnPauseRequested, which is required before exporting.",
                ContextObject = null
            }
        };
    }
}
