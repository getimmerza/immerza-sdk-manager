using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[CheckableAttribute(displayName: "LUA EndScene Check")]
public class EndSceneChecker : ICheckable
{
    public void RunCheck(CheckContext context)
    {
        List<LuaAsset> scripts = CheckUtil.GetLuaAssets();

        Regex regex = new("EndScene\\(\\)");

        foreach (LuaAsset script in scripts)
        {
            if (regex.IsMatch(script.content))
            {
                return;
            }
        }

        context.AddError("None of the Lua scripts call EndScene(), which is required before exporting.");
    }
}
