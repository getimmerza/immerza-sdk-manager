using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CheckableAttribute(displayName: "Lua Manager Check")]
public class LuaManagerChecker : ICheckable
{
    public void RunCheck(CheckContext context)
    {
        ImmerzaLuaManager[] found = Object.FindObjectsByType<ImmerzaLuaManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (found.Length == 0)
        {
            context.AddError("ImmerzaLuaManager not found inside scene! Add an empty GameObject to your scene and add the component.");
        }
        else if (found.Length > 1)
        {
            context.AddWarning($"Multiple ImmerzaLuaManagers found inside scene! Remove the duplicates: {string.Join(", ", found.Select(obj => obj.name))}");
        }
        else if (!found[0].isActiveAndEnabled)
        {
            context.AddWarning($"ImmerzaLuaManager inactive, consider reactivating it before exporting.", found[0]);
        }
    }
}
