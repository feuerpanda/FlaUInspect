using System;
using System.Drawing;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Exceptions;

namespace FlaUInspect.Core
{
    public static class ElementHighlighter
    {
        public static void HighlightElement(AutomationElement automationElement)
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        automationElement.DrawHighlight(false, Color.Red, TimeSpan.FromSeconds(2));
                    }
                    catch { }
                });
            }
            catch (PropertyNotSupportedException ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}
