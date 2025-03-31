using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Linq;
using UnityEngine;

public class LuaManagerChecker : ICheckable
{
    CheckResult ICheckable.RunCheck()
    {
        ImmerzaLuaManager[] found = Object.FindObjectsByType<ImmerzaLuaManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (found.Length == 0)
        {
            return new CheckResult()
            { 
                Type = ResultType.Error,
                Message = "ImmerzaLuaManager not found inside scene! Add an empty GameObject to your scene and add the component.",
                ContextObject = null
            };
        }

        if (found.Length > 1)
        {
            return new CheckResult()
            {
                Type = ResultType.Warning,
                Message = $"Multiple ImmerzaLuaManagers found inside scene! Remove the duplicates: {string.Join(", ", found.Select(obj => obj.name))}",
                ContextObject = null
            };
        }

        if (!found[0].isActiveAndEnabled)
        {
            return new CheckResult()
            {
                Type = ResultType.Warning,
                Message = $"ImmerzaLuaManager inactive, consider reactivating it before exporting.",
                ContextObject = found[0]
            };
        }

        return new CheckResult()
        {
            Type = ResultType.Success,
            Message = "ImmerzaLuaManager check succeeded.",
            ContextObject = found[0]
        };
    }
}
