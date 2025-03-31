using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    public class PreflightCheckManager
    {
        [MenuItem("Immerza/Run Preflight Checks")]
        public static void RunChecks()
        {
            List<CheckResult> checkResults = new();

            List<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ICheckable).IsAssignableFrom(p) && p.IsClass).ToList();

            foreach (Type crtType in types)
            {
                ICheckable handlerInstance = (ICheckable)Activator.CreateInstance(crtType);
                CheckResult checkResult = handlerInstance.RunCheck();
                checkResults.Add(checkResult);
                
                switch (checkResult.Type)
                {
                    case ResultType.Success:
                        Debug.Log($"{checkResult.Message}", checkResult.ContextObject);
                        break;
                    case ResultType.Warning:
                        Debug.LogWarning($"{checkResult.Message}", checkResult.ContextObject);
                        break;
                    case ResultType.Error:
                        Debug.LogError($"{checkResult.Message}", checkResult.ContextObject);
                        break;
                }
            }

            Debug.Log($"Ran {checkResults.Count} test{(checkResults.Count > 1 ? "s" : "")}");
        }
    }
}
