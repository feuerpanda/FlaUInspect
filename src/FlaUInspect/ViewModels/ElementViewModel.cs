﻿using FlaUI.Core;
using FlaUI.Core.Conditions;
using FlaUI.Core.Identifiers;
using FlaUI.Core.Patterns;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using FlaUI.UIA3.Identifiers;
using FlaUInspect.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Input;
using AutomationElement = FlaUI.Core.AutomationElements.AutomationElement;
using CacheRequest = FlaUI.Core.CacheRequest;
using ControlType = FlaUI.Core.Definitions.ControlType;
using TreeScope = FlaUI.Core.Definitions.TreeScope;

namespace FlaUInspect.ViewModels;

public class ElementViewModel : ObservableObject
{
    public ElementViewModel(AutomationElement automationElement, MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
        AutomationElement = automationElement;
        Children = new ExtendedObservableCollection<ElementViewModel>();
        ItemDetails = new ExtendedObservableCollection<DetailGroupViewModel>();
        HighlightCommand = new RelayCommand(_ => ElementHighlighter.HighlightElement(AutomationElement));
        FocusCommand = new RelayCommand(_ =>
        {
            try
            {
                AutomationElement.Focus();
            }
            catch
            {
            }
        });
        ClickCommand = new RelayCommand(_ =>
        {
            try
            {
                AutomationElement.Click();
            }
            catch
            {
            }
        });
        DoubleClickCommand = new RelayCommand(_ =>
        {
            try
            {
                AutomationElement.DoubleClick();
            }
            catch
            {
            }
        });
        RightClickCommand = new RelayCommand(_ =>
        {
            try
            {
                AutomationElement.RightClick();
            }
            catch
            {
            }
        });
    }

    public AutomationElement AutomationElement { get; }
    public MainViewModel MainViewModel { get; set; }

    public ICommand HighlightCommand { get; }
    public ICommand FocusCommand { get; }
    public ICommand ClickCommand { get; }
    public ICommand DoubleClickCommand { get; }
    public ICommand RightClickCommand { get; }

    public bool IsSelected
    {
        get => this.GetProperty<bool>();
        set
        {
            try
            {
                if (value)
                {
                    if (MainViewModel.EnableHighlightOnSelectionChanged)
                        ElementHighlighter.HighlightElement(AutomationElement);

                    // Async load details
                    Task unused = Task.Run(() =>
                    {
                        List<DetailGroupViewModel> details = LoadDetails();
                        return details;
                    }).ContinueWith(items =>
                    {
                        if (items.IsFaulted && items.Exception != null)
                        {
                            //MessageBox.Show(items.Exception.ToString());
                            ErrorsLogsViewModel.Instance.Log(items.Exception);
                        }
                        ItemDetails.Reset(items.Result);
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                    // Fire the selection event
                    SelectionChanged?.Invoke(this);
                }

                this.SetProperty(value);
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                ErrorsLogsViewModel.Instance.Log(ex);
            }
        }
    }

    public bool IsExpanded
    {
        get => this.GetProperty<bool>();
        set
        {
            this.SetProperty(value);
            if (value)
            {
                LoadChildren(true);
            }
        }
    }

    public string Name => NormalizeString(AutomationElement.Properties.Name.ValueOrDefault);

    public string AutomationId => NormalizeString(AutomationElement.Properties.AutomationId.ValueOrDefault);

    public ControlType ControlType => AutomationElement.Properties.ControlType.TryGetValue(out ControlType value) ? value : ControlType.Custom;

    public ExtendedObservableCollection<ElementViewModel> Children { get; set; }

    public ExtendedObservableCollection<DetailGroupViewModel> ItemDetails { get; set; }

    public string XPath
    {
        get
        {
            try
            {
                return Debug.GetXPathToElement(AutomationElement);
            }
            catch
            {
                return "- Error -";
            }
        }
    }

    public event Action<ElementViewModel> SelectionChanged;

    public void LoadChildren(bool loadInnerChildren)
    {
        foreach (ElementViewModel child in Children)
        {
            child.SelectionChanged -= SelectionChanged;
        }

        List<ElementViewModel> childrenViewModels = new();
        try
        {
            foreach (AutomationElement child in AutomationElement.FindAllChildren())
            {
                var childViewModel = new ElementViewModel(child, MainViewModel);
                childViewModel.SelectionChanged += SelectionChanged;
                childrenViewModels.Add(childViewModel);

                if (loadInnerChildren)
                {
                    childViewModel.LoadChildren(false);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }

        Children.Reset(childrenViewModels);
    }

    private List<DetailGroupViewModel> LoadDetails()
    {
        List<DetailGroupViewModel> detailGroups = new();
        var cacheRequest = new CacheRequest
        {
            TreeScope = TreeScope.Element
        };
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.AutomationId);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.Name);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.ClassName);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.ControlType);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.LocalizedControlType);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.FrameworkId);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.ProcessId);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.IsEnabled);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.IsOffscreen);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.BoundingRectangle);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.HelpText);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.IsPassword);
        cacheRequest.Add(AutomationElement.Automation.PropertyLibrary.Element.NativeWindowHandle);
        using (cacheRequest.Activate())
        {
            AutomationElement elementCached = AutomationElement.FindFirst(TreeScope.Element, TrueCondition.Default);
            if (elementCached != null)
            {
                // Element identification
                List<IDetailViewModel> identification = new()
                {
                    DetailViewModel.FromAutomationProperty("AutomationId", elementCached.Properties.AutomationId),
                    DetailViewModel.FromAutomationProperty("Name", elementCached.Properties.Name),
                    DetailViewModel.FromAutomationProperty("ClassName", elementCached.Properties.ClassName),
                    DetailViewModel.FromAutomationProperty("ControlType", elementCached.Properties.ControlType),
                    DetailViewModel.FromAutomationProperty("LocalizedControlType", elementCached.Properties.LocalizedControlType),
                    new DetailViewModel("FrameworkType", elementCached.FrameworkType.ToString()),
                    DetailViewModel.FromAutomationProperty("FrameworkId", elementCached.Properties.FrameworkId),
                    DetailViewModel.FromAutomationProperty("ProcessId", elementCached.Properties.ProcessId)
                };
                detailGroups.Add(new DetailGroupViewModel("Identification", identification));

                // Element details
                List<DetailViewModel> details = new()
                {
                    DetailViewModel.FromAutomationProperty("IsEnabled", elementCached.Properties.IsEnabled),
                    DetailViewModel.FromAutomationProperty("IsOffscreen", elementCached.Properties.IsOffscreen),
                    DetailViewModel.FromAutomationProperty("BoundingRectangle", elementCached.Properties.BoundingRectangle),
                    DetailViewModel.FromAutomationProperty("HelpText", elementCached.Properties.HelpText),
                    DetailViewModel.FromAutomationProperty("IsPassword", elementCached.Properties.IsPassword)
                };
                // Special handling for NativeWindowHandle
                IntPtr nativeWindowHandle = elementCached.Properties.NativeWindowHandle.ValueOrDefault;
                string nativeWindowHandleString = "Not Supported";
                if (nativeWindowHandle != default(IntPtr))
                {
                    nativeWindowHandleString = string.Format("{0} ({0:X8})", nativeWindowHandle.ToInt32());
                }
                details.Add(new DetailViewModel("NativeWindowHandle", nativeWindowHandleString));
                detailGroups.Add(new DetailGroupViewModel("Details", details));
            }
        }

        // Pattern details
        PatternId[] allSupportedPatterns = AutomationElement.GetSupportedPatterns();
        PatternId[] allPatterns = AutomationElement.Automation.PatternLibrary.AllForCurrentFramework;
        List<DetailViewModel> patterns = new();
        foreach (PatternId pattern in allPatterns)
        {
            bool hasPattern = allSupportedPatterns.Contains(pattern);
            patterns.Add(new DetailViewModel(pattern.Name + "Pattern", hasPattern ? "Yes" : "No") { Important = hasPattern });
        }
        detailGroups.Add(new DetailGroupViewModel("Pattern Support", patterns));

        // GridItemPattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.GridItemPattern))
        {
            IGridItemPattern pattern = AutomationElement.Patterns.GridItem.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("Column", pattern.Column),
                DetailViewModel.FromAutomationProperty("ColumnSpan", pattern.ColumnSpan),
                DetailViewModel.FromAutomationProperty("Row", pattern.Row),
                DetailViewModel.FromAutomationProperty("RowSpan", pattern.RowSpan),
                DetailViewModel.FromAutomationProperty("ContainingGrid", pattern.ContainingGrid)
            };
            detailGroups.Add(new DetailGroupViewModel("GridItem Pattern", patternDetails));
        }
        // GridPattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.GridPattern))
        {
            IGridPattern pattern = AutomationElement.Patterns.Grid.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("ColumnCount", pattern.ColumnCount),
                DetailViewModel.FromAutomationProperty("RowCount", pattern.RowCount)
            };
            detailGroups.Add(new DetailGroupViewModel("Grid Pattern", patternDetails));
        }
        // LegacyIAccessiblePattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.LegacyIAccessiblePattern))
        {
            ILegacyIAccessiblePattern pattern = AutomationElement.Patterns.LegacyIAccessible.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("Name", pattern.Name),
                new DetailViewModel("State", AccessibilityTextResolver.GetStateText(pattern.State.ValueOrDefault)),
                new DetailViewModel("Role", AccessibilityTextResolver.GetRoleText(pattern.Role.ValueOrDefault)),
                DetailViewModel.FromAutomationProperty("Value", pattern.Value),
                DetailViewModel.FromAutomationProperty("ChildId", pattern.ChildId),
                DetailViewModel.FromAutomationProperty("DefaultAction", pattern.DefaultAction),
                DetailViewModel.FromAutomationProperty("Description", pattern.Description),
                DetailViewModel.FromAutomationProperty("Help", pattern.Help),
                DetailViewModel.FromAutomationProperty("KeyboardShortcut", pattern.KeyboardShortcut),
                DetailViewModel.FromAutomationProperty("Selection", pattern.Selection)
            };
            detailGroups.Add(new DetailGroupViewModel("LegacyIAccessible Pattern", patternDetails));
        }
        // RangeValuePattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.RangeValuePattern))
        {
            IRangeValuePattern pattern = AutomationElement.Patterns.RangeValue.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("IsReadOnly", pattern.IsReadOnly),
                DetailViewModel.FromAutomationProperty("SmallChange", pattern.SmallChange),
                DetailViewModel.FromAutomationProperty("LargeChange", pattern.LargeChange),
                DetailViewModel.FromAutomationProperty("Minimum", pattern.Minimum),
                DetailViewModel.FromAutomationProperty("Maximum", pattern.Maximum),
                DetailViewModel.FromAutomationProperty("Value", pattern.Value)
            };
            detailGroups.Add(new DetailGroupViewModel("RangeValue Pattern", patternDetails));
        }
        // ScrollPattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.ScrollPattern))
        {
            IScrollPattern pattern = AutomationElement.Patterns.Scroll.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("HorizontalScrollPercent", pattern.HorizontalScrollPercent),
                DetailViewModel.FromAutomationProperty("HorizontalViewSize", pattern.HorizontalViewSize),
                DetailViewModel.FromAutomationProperty("HorizontallyScrollable", pattern.HorizontallyScrollable),
                DetailViewModel.FromAutomationProperty("VerticalScrollPercent", pattern.VerticalScrollPercent),
                DetailViewModel.FromAutomationProperty("VerticalViewSize", pattern.VerticalViewSize),
                DetailViewModel.FromAutomationProperty("VerticallyScrollable", pattern.VerticallyScrollable)
            };
            detailGroups.Add(new DetailGroupViewModel("Scroll Pattern", patternDetails));
        }
        // SelectionItemPattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.SelectionItemPattern))
        {
            ISelectionItemPattern pattern = AutomationElement.Patterns.SelectionItem.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("IsSelected", pattern.IsSelected),
                DetailViewModel.FromAutomationProperty("SelectionContainer", pattern.SelectionContainer)
            };
            detailGroups.Add(new DetailGroupViewModel("SelectionItem Pattern", patternDetails));
        }
        // SelectionPattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.SelectionPattern))
        {
            ISelectionPattern pattern = AutomationElement.Patterns.Selection.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("Selection", pattern.Selection),
                DetailViewModel.FromAutomationProperty("CanSelectMultiple", pattern.CanSelectMultiple),
                DetailViewModel.FromAutomationProperty("IsSelectionRequired", pattern.IsSelectionRequired)
            };
            detailGroups.Add(new DetailGroupViewModel("Selection Pattern", patternDetails));
        }
        // TableItemPattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.TableItemPattern))
        {
            ITableItemPattern pattern = AutomationElement.Patterns.TableItem.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("ColumnHeaderItems", pattern.ColumnHeaderItems),
                DetailViewModel.FromAutomationProperty("RowHeaderItems", pattern.RowHeaderItems)
            };
            detailGroups.Add(new DetailGroupViewModel("TableItem Pattern", patternDetails));
        }
        // TablePattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.TablePattern))
        {
            ITablePattern pattern = AutomationElement.Patterns.Table.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("ColumnHeaderItems", pattern.ColumnHeaders),
                DetailViewModel.FromAutomationProperty("RowHeaderItems", pattern.RowHeaders),
                DetailViewModel.FromAutomationProperty("RowOrColumnMajor", pattern.RowOrColumnMajor)
            };
            detailGroups.Add(new DetailGroupViewModel("Table Pattern", patternDetails));
        }
        // TextPattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.TextPattern))
        {
            ITextPattern pattern = AutomationElement.Patterns.Text.Pattern;

            // TODO: This can in the future be replaced with automation.MixedAttributeValue
            object mixedValue = AutomationElement.AutomationType == AutomationType.UIA2
            ? TextPattern.MixedAttributeValue
            : ((UIA3Automation)AutomationElement.Automation).NativeAutomation.ReservedMixedAttributeValue;

            string foreColor = GetTextAttribute<int>(pattern, TextAttributes.ForegroundColor, mixedValue, x => $"{Color.FromArgb(x)} ({x})");
            string backColor = GetTextAttribute<int>(pattern, TextAttributes.BackgroundColor, mixedValue, x => $"{Color.FromArgb(x)} ({x})");
            string fontName = GetTextAttribute<string>(pattern, TextAttributes.FontName, mixedValue, x => $"{x}");
            string fontSize = GetTextAttribute<double>(pattern, TextAttributes.FontSize, mixedValue, x => $"{x}");
            string fontWeight = GetTextAttribute<int>(pattern, TextAttributes.FontWeight, mixedValue, x => $"{x}");

            List<DetailViewModel> patternDetails = new List<DetailViewModel>
            {
                new("ForeColor", foreColor),
                new("BackgroundColor", backColor),
                new("FontName", fontName),
                new("FontSize", fontSize),
                new("FontWeight", fontWeight)
            };
            detailGroups.Add(new DetailGroupViewModel("Text Pattern", patternDetails));
        }
        // TogglePattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.TogglePattern))
        {
            ITogglePattern pattern = AutomationElement.Patterns.Toggle.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("ToggleState", pattern.ToggleState)
            };
            detailGroups.Add(new DetailGroupViewModel("Toggle Pattern", patternDetails));
        }
        // ValuePattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.ValuePattern))
        {
            IValuePattern pattern = AutomationElement.Patterns.Value.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("IsReadOnly", pattern.IsReadOnly),
                DetailViewModel.FromAutomationProperty("Value", pattern.Value)
            };
            detailGroups.Add(new DetailGroupViewModel("Value Pattern", patternDetails));
        }
        // WindowPattern
        if (allSupportedPatterns.Contains(AutomationElement.Automation.PatternLibrary.WindowPattern))
        {
            IWindowPattern pattern = AutomationElement.Patterns.Window.Pattern;
            List<DetailViewModel> patternDetails = new()
            {
                DetailViewModel.FromAutomationProperty("IsModal", pattern.IsModal),
                DetailViewModel.FromAutomationProperty("IsTopmost", pattern.IsTopmost),
                DetailViewModel.FromAutomationProperty("CanMinimize", pattern.CanMinimize),
                DetailViewModel.FromAutomationProperty("CanMaximize", pattern.CanMaximize),
                DetailViewModel.FromAutomationProperty("WindowVisualState", pattern.WindowVisualState),
                DetailViewModel.FromAutomationProperty("WindowInteractionState", pattern.WindowInteractionState)
            };
            detailGroups.Add(new DetailGroupViewModel("Window Pattern", patternDetails));
        }

        return detailGroups;
    }

    private string GetTextAttribute<T>(ITextPattern pattern, TextAttributeId textAttribute, object mixedValue, Func<T, string> func)
    {
        object value = pattern.DocumentRange.GetAttributeValue(textAttribute);

        if (value == mixedValue)
        {
            return "Mixed";
        }
        if (value == AutomationElement.Automation.NotSupportedValue)
        {
            return "Not supported";
        }
        try
        {
            var converted = (T)value;
            return func(converted);
        }
        catch
        {
            return $"Conversion to ${typeof(T)} failed";
        }
    }

    private string NormalizeString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }
        return value.Replace(Environment.NewLine, " ").Replace('\r', ' ').Replace('\n', ' ');
    }
}