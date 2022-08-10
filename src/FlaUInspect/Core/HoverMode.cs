using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUInspect.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Mouse = FlaUI.Core.Input.Mouse;
using Point = System.Drawing.Point;

namespace FlaUInspect.Core;

public class HoverMode
{
    private readonly AutomationBase _automation;
    private readonly DispatcherTimer _dispatcherTimer;
    private AutomationElement _currentHoveredElement;

    public HoverMode(AutomationBase automation)
    {
        _automation = automation;
        _dispatcherTimer = new DispatcherTimer();
        _dispatcherTimer.Tick += DispatcherTimerTick;
        _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(500);
    }

    public event Action<AutomationElement> ElementHovered;

    public void Start()
    {
        _currentHoveredElement = null;
        _dispatcherTimer.Start();
    }

    public void Stop()
    {
        _currentHoveredElement = null;
        _dispatcherTimer.Stop();
    }

    private void DispatcherTimerTick(object sender, EventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            Point screenPos = Mouse.Position;
            try
            {
                AutomationElement hoveredElement = _automation.FromPoint(screenPos);
                // Skip items in the current process
                // Like Inspect itself or the overlay window
                if (hoveredElement.Properties.ProcessId == Process.GetCurrentProcess().Id)
                {
                    return;
                }
                if (!Equals(_currentHoveredElement, hoveredElement))
                {
                    _currentHoveredElement = hoveredElement;
                    ElementHovered?.Invoke(hoveredElement);
                }
                else
                {
                    ElementHighlighter.HighlightElement(hoveredElement);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                string caption = "FlaUInspect - Unauthorized access exception";
                string message = "You are accessing a protected UI element in hover mode.\nTry to start FlaUInspect as administrator.";
                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                ErrorsLogsViewModel.Instance.Log(ex);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                ErrorsLogsViewModel.Instance.Log(ex);
            }
        }
    }
}