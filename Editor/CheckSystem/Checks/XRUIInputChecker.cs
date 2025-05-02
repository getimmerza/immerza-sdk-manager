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
            context.AddError("No NearFarInteractors found! Scene needs atleast two set to the 'UI' layer.");
        }
        else if (found.Length == 1)
        {
            if ((found[0].interactionLayers.value & InteractionLayerMask.GetMask("UI")) == 0)
            {
                context.AddError("Not one NearFarInteractor with the 'UI' interaction layer has been found! Scene needs atleast two set to the 'UI' layer.");
            }
        }
        else
        {
            int foundInteractors = 0;

            foreach (NearFarInteractor interactor in found)
            {
                if ((interactor.interactionLayers.value & InteractionLayerMask.GetMask("UI")) != 0)
                {
                    foundInteractors++;
                }
            }

            if (foundInteractors < 2)
            {
                context.AddError("Only one NearFarInteractor with the 'UI' interaction layer has been found! Scene needs atleast two set to the 'UI' layer.", found[0]);
            }
        }
    }
}
