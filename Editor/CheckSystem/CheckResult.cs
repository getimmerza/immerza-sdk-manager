namespace ImmerzaSDK.Manager.Editor
{
    internal enum ResultType
    {
        Success,
        Warning,
        Error
    }

    internal class CheckResult
    {
        public ResultType Type { get; set; }
        public string Message { get; set; }
        public UnityEngine.Object ContextObject { get; set; }
    }
}
