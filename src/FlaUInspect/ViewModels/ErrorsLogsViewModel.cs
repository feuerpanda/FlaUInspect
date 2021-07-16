using FlaUInspect.Core;
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FlaUInspect.ViewModels
{
    public class ErrorsLogsViewModel
    {
        public ObservableCollection<ErrorLog> Logs { get; } = new ObservableCollection<ErrorLog>();

        public void Log(Exception exception, [CallerMemberName]string callerMethodOrPropertyName = "")
        {
            Logs.Insert(0, new ErrorLog(callerMethodOrPropertyName, exception));
        }

        public ICommand ClearCommand => new RelayCommand(_ => Clear());

        public void Clear()
        {
            Logs.Clear();
        }

        #region Singleton          

        private static ErrorsLogsViewModel instance;

        public static ErrorsLogsViewModel Instance => instance ?? (instance = new ErrorsLogsViewModel());

        private ErrorsLogsViewModel()
        { }

        #endregion
    }
}
