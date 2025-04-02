using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    public static class PreflightCheckManager
    {
        public static event Action<List<CheckResult>> OnLogCheck;

        public static void RunChecks()
        {
            List<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ICheckable).IsAssignableFrom(p) && p.IsClass).ToList();

            foreach (Type crtType in types)
            {
                ICheckable handlerInstance = (ICheckable)Activator.CreateInstance(crtType);
                List<CheckResult> checkResultRes = handlerInstance.RunCheck();
                
                foreach (CheckResult checkResult in checkResultRes)
                {
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

                OnLogCheck?.Invoke(checkResultRes);
            }
        }
    }
}
