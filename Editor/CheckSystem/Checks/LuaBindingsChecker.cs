using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[CheckableAttribute(displayName: "Lua Bindings Check")]
public class LuaBindingsChecker : ICheckable
{
    private readonly Regex TypeRegex = new(@"\bCS\.[A-Za-z_][A-Za-z0-9_.]*\b", RegexOptions.Compiled);
    public void RunCheck(CheckContext context)
    {
        HashSet<string> luaBindings = CheckUtil.GetAllLuaBindingNames();
        List<LuaAsset> luaScripts = CheckUtil.GetLuaAssets();
        
        foreach (LuaAsset script in luaScripts)
        {
            string content = script.content;

            foreach (Match match in TypeRegex.Matches(content))
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
