using ImmerzaSDK.Lua;
using ImmerzaSDK.Manager.Editor;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static UnityEditor.Experimental.GraphView.GraphView;

public class XRUIInputChecker : ICheckable
{
    CheckResult ICheckable.RunCheck()
    {
        NearFarInteractor[] found = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (found.Length == 0)
        {
            return new CheckResult()
            { 
                Type = ResultType.Error,
                Message = "No NearFarInteractors found! Scene needs atleast two set to the 'UI' layer.",
                ContextObject = null
            };
        }

        if (found.Length == 1)
        {
            if ((found[0].interactionLayers.value & (1 << InteractionLayerMask.GetMask("UI"))) == 0)
            {
                return new CheckResult()
                {
                    Type = ResultType.Error,
                    Message = "Not one NearFarInteractor with the 'UI' interaction layer has been found! Scene needs atleast two set to the 'UI' layer.",
                    ContextObject = null
                };
            }
        }

        int foundInteractors = 0;

        foreach (NearFarInteractor interactor in found)
        {
            if ((interactor.interactionLayers.value & (1 << InteractionLayerMask.GetMask("UI"))) != 0)
            {
                foundInteractors++;
            }
        }

        if (foundInteractors >= 2)
        {
            return new CheckResult()
            {
                Type = ResultType.Success,
                Message = "XR UI Input check succeeded.",
                ContextObject = null
            };
        }
        else
        {
            return new CheckResult()
            {
                Type = ResultType.Error,
                Message = "Only one NearFarInteractor with the 'UI' interaction layer has been found! Scene needs atleast two set to the 'UI' layer.",
                ContextObject = found[0]
            };
        }
    }
}
