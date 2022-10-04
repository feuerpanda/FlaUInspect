using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.EventHandlers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Application = System.Windows.Application;

namespace FlaUInspect.Core;

public class FocusTrackingMode
{
    private readonly AutomationBase _automation;
    private AutomationElement _currentFocusedElement;
    private FocusChangedEventHandlerBase _eventHandler;

    public FocusTrackingMode(AutomationBase automation)
    {
        _automation = automation;
    }

    public event Action<AutomationElement> ElementFocused;

    public void Start()
    {
        // Might give problems because inspect is registered as well.
        // MS recommends to call UIA commands on a thread outside of an UI thread.
        Task.Factory.StartNew(() => _eventHandler = _automation.RegisterFocusChangedEvent(this.OnFocusChanged));
    }

    public void Stop()
    {
        _automation.UnregisterFocusChangedEvent(_eventHandler);
    }

    private void OnFocusChanged(AutomationElement automationElement)
    {
        // Skip items in the current process
        // Like Inspect itself or the overlay window
        if (automationElement.Properties.ProcessId == Process.GetCurrentProcess().Id)
        {
            return;
        }
        if (!Equals(_currentFocusedElement, automationElement))
        {
            _currentFocusedElement = automationElement;
            Application.Current?.Dispatcher?.Invoke(() => ElementFocused?.Invoke(automationElement));
        }
    }
}