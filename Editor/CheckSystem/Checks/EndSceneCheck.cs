using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class EndSceneChecker : ICheckable
{
    CheckResult ICheckable.RunCheck()
    {
        List<LuaAsset> scripts = CheckUtil.GetLuaAssets();

        Regex regex = new("EndScene\\(\\)");

        foreach (LuaAsset script in scripts)
        {
            if (regex.IsMatch(script.content))
            {
                return new CheckResult()
                {
                    Type = ResultType.Success,
                    Message = "EndScene() check succeeded",
                    ContextObject = null
                };
            }
        }

        return new CheckResult()
        {
            Type = ResultType.Error,
            Message = "None of the Lua scripts call EndScene(), which is required before exporting.",
            ContextObject = null
        };
    }
}
