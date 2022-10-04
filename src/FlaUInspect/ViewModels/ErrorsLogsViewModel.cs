using FlaUInspect.Core;
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FlaUInspect.ViewModels;

public class ErrorsLogsViewModel
{
    public ObservableCollection<ErrorLog> Logs { get; } = new();

    public ICommand ClearCommand => new RelayCommand(_ => this.Clear());

    public void Log(Exception exception, [CallerMemberName] string callerMethodOrPropertyName = "")
    {
        this.Logs.Insert(0, new ErrorLog(callerMethodOrPropertyName, exception));
    }

    public void Clear()
    {
        this.Logs.Clear();
    }

    #region Singleton

    private static ErrorsLogsViewModel instance;

    public static ErrorsLogsViewModel Instance => ErrorsLogsViewModel.instance ?? (ErrorsLogsViewModel.instance = new ErrorsLogsViewModel());

    private ErrorsLogsViewModel()
    {
    }

    #endregion Singleton
}