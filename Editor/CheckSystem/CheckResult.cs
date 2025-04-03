using System.Collections.Generic;
using System.Linq;

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

    public class CheckContext
    {
        private List<CheckResult> _results = new List<CheckResult>();
        private int _numWarnings = 0;
        private int _numErrors = 0;

        public IEnumerable<CheckResult> Results { get => _results; }
        public bool HasWarnings { get => _numWarnings > 0; }
        public bool HasErrors {  get => _numErrors > 0; }
        public bool AllSucceeded {  get => !HasWarnings && !HasErrors; }

        public void Reset()
        {
            _results.Clear();
            _numWarnings = 0;
            _numErrors = 0;
        }

        public void AddError(string message)
        {
            AddError(message, null);
        }
        public void AddError(string message, UnityEngine.Object contextObject)
        {
            AddResult(message, ResultType.Error, contextObject);
        }
        public void AddWarning(string message)
        {
            AddWarning(message, null);
        }
        public void AddWarning(string message, UnityEngine.Object contextObject)
        {
            AddResult(message, ResultType.Warning, contextObject);
        }
        public void AddSuccess(string message)
        {
            AddSuccess(message, null);
        }
        public void AddSuccess(string message, UnityEngine.Object contextObject)
        {
            AddResult(message, ResultType.Success, contextObject);
        }
        public void AddResult(string message, ResultType type, UnityEngine.Object contextObject)
        {
            switch (type)
            {
                case ResultType.Error:   ++_numErrors; break;
                case ResultType.Warning: ++_numWarnings; break;
            }
            _results.Add(new CheckResult { Type = type, Message = message, ContextObject = contextObject });
        }
    }
}
