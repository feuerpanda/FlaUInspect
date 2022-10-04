using System;

namespace FlaUInspect.ViewModels;

public class ErrorLog
{
    public ErrorLog(string callerName, Exception exception)
    {
        this.CallerName = callerName;
        this.Exception = exception;
    }

    public string CallerName { get; }

    public string Text => this.Exception.ToString();

    public Exception Exception { get; }
}