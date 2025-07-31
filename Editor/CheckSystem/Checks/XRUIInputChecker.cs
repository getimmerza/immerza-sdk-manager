using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static UnityEditor.Experimental.GraphView.GraphView;

[CheckableAttribute(displayName: "XR UI Input Check")]
public class XRUIInputChecker : ICheckable
{
    public void RunCheck(CheckContext context)
    {
        NearFarInteractor[] found = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (found.Length == 0)
        {
            context.AddError("No NearFarInteractors found! Scene needs atleast one for each hand.");
        }
        else if (found.Length == 1)
        {
            context.AddError("Only one NearFarInteractor found! Scene needs atleast one for each hand.");
        }

        foreach (NearFarInteractor comp in found)
        {
            if (!comp.gameObject.activeInHierarchy)
            {
                context.AddWarning($"NearFarInteractor on GameObject '{comp.gameObject.name}' is inactive, be sure to enable it at runtime or before exporting.", comp);
            }
        }
    }
}
