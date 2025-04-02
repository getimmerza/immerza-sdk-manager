namespace ImmerzaSDK.Manager.Editor
{
    public enum ResultType
    {
        Success,
        Warning,
        Error
    }

    public class CheckResult
    {
        public ResultType Type { get; set; }
        public string Message { get; set; }
        public UnityEngine.Object ContextObject { get; set; }
    }
}
