using System;

namespace FlaUInspect.ViewModels
{
    public class ErrorLog
    {
        public ErrorLog(string callerName, Exception exception)
        {
            CallerName = callerName;
            Exception = exception;
        }

        public string CallerName { get; }

        public string Text => Exception.ToString();

        public Exception Exception { get; }
    }
}