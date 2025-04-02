using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    public static class PreflightCheckManager
    {
        public static event Action OnBeforeRunChecks;
        public static event Action<ResultType, string> OnLogCheck;

        public static void RunChecks()
        {
            List<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ICheckable).IsAssignableFrom(p) && p.IsClass).ToList();

            CheckContext checkContext = new CheckContext();

            OnBeforeRunChecks?.Invoke();

            foreach (Type crtType in types)
            {
                string displayName = crtType.Name;

                foreach (Attribute attribute in Attribute.GetCustomAttributes(crtType))
                {
                    if (attribute is CheckableAttribute checkableAttribute)
                    {
                        displayName = checkableAttribute.DisplayName;
                        break;
                    }
                }

                ICheckable handlerInstance = (ICheckable)Activator.CreateInstance(crtType);

                checkContext.Reset();
                handlerInstance.RunCheck(checkContext);

                if (checkContext.Results.Count() == 0)
                {
                    DispatchResult(ResultType.Success, $"{displayName}: succeeded", null);
                }
                else
                {
                    foreach (CheckResult checkResult in checkContext.Results)
                    {
                        DispatchResult(checkResult.Type, $"{displayName}: {checkResult.Message}", checkResult.ContextObject);
                    }
                }
            }
        }

        private static void DispatchResult(ResultType type, string message, UnityEngine.Object contextObject)
        {
            OnLogCheck?.Invoke(type, message);

            switch (type)
            {
                case ResultType.Success:
                    Debug.Log(message, contextObject);
                    break;
                case ResultType.Warning:
                    Debug.LogWarning(message, contextObject);
                    break;
                case ResultType.Error:
                    Debug.LogError(message, contextObject);
                    break;
            }
        }
    }
}
