using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace ImmerzaSDK.Manager.Editor
{
    internal interface ICheckable
    {
        void RunCheck(CheckContext context);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class CheckableAttribute : Attribute
    {
        public CheckableAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; private set; }
    }
}