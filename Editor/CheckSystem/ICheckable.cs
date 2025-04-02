using System.Collections.Generic;

namespace ImmerzaSDK.Manager.Editor
{
    internal interface ICheckable
    {
        List<CheckResult> RunCheck();
    }
}