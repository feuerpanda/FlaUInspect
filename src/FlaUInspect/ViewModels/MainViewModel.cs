using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA2;
using FlaUI.UIA3;
using FlaUInspect.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FlaUInspect.ViewModels;

public class MainViewModel : ObservableObject
{
    private AutomationBase _automation;
    private FocusTrackingMode _focusTrackingMode;
    private HoverMode _hoverMode;
    private AutomationElement _rootElement;
    private ITreeWalker _treeWalker;

    public MainViewModel()
    {
        Elements = new ObservableCollection<ElementViewModel>();
        StartNewInstanceCommand = new RelayCommand(_ =>
        {
            var info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
            Process.Start(info);
        });
        SaveCaptureSelectedItemCommand = new RelayCommand(_ =>
        {
            if (SelectedItemInTree == null)
            {
                return;
            }
            Bitmap capturedImage = SelectedItemInTree.AutomationElement.Capture();
            var saveDialog = new SaveFileDialog
            {
                Filter = "Png file (*.png)|*.png"
            };
            if (saveDialog.ShowDialog() == true)
            {
                capturedImage.Save(saveDialog.FileName, ImageFormat.Png);
            }
            capturedImage.Dispose();
        });
        CaptureSelectedItemToClipboard = new RelayCommand(_ =>
        {
            if (SelectedItemInTree == null)
            {
                return;
            }
            Clipboard.SetImage(SelectedItemInTree.AutomationElement.Capture());
        });
        RefreshCommand = new RelayCommand(_ => RefreshTree());
        SwitchHoverModeCommand = new RelayCommand(_ => EnableHoverMode = !EnableHoverMode);
        SwitchFocusModeCommand = new RelayCommand(_ => EnableFocusTrackingMode = !EnableFocusTrackingMode);
        SwitchXPathModeCommand = new RelayCommand(_ => EnableXPath = !EnableXPath);

        EnableXPath = true;
    }

    public bool IsInitialized
    {
        get => this.GetProperty<bool>();
        private set => this.SetProperty(value);
    }

    public bool EnableHoverMode
    {
        get => this.GetProperty<bool>();
        set
        {
            if (this.SetProperty(value))
            {
                if (value)
                {
                    _hoverMode.Start();
                }
                else
                {
                    _hoverMode.Stop();
                }
            }
        }
    }

    public bool EnableFocusTrackingMode
    {
        get => this.GetProperty<bool>();
        set
        {
            if (this.SetProperty(value))
            {
                if (value)
                {
                    _focusTrackingMode.Start();
                }
                else
                {
                    _focusTrackingMode.Stop();
                }
            }
        }
    }

    public bool EnableHighlightOnSelectionChanged
    {
        get => this.GetProperty<bool>();
        set => this.SetProperty(value);
    }

    public bool EnableXPath
    {
        get => this.GetProperty<bool>();
        set => this.SetProperty(value);
    }

    public AutomationType SelectedAutomationType
    {
        get => this.GetProperty<AutomationType>();
        private set => this.SetProperty(value);
    }

    public ObservableCollection<ElementViewModel> Elements { get; }

    public ICommand StartNewInstanceCommand { get; }

    public ICommand SaveCaptureSelectedItemCommand { get; }

    public ICommand CaptureSelectedItemToClipboard { get; }

    public ICommand RefreshCommand { get; }

    public ICommand SwitchHoverModeCommand { get; }
    public ICommand SwitchFocusModeCommand { get; }
    public ICommand SwitchXPathModeCommand { get; }

    public ObservableCollection<DetailGroupViewModel> SelectedItemDetails => SelectedItemInTree?.ItemDetails;

    public ElementViewModel SelectedItemInTree
    {
        get => this.GetProperty<ElementViewModel>();
        private set => this.SetProperty(value);
    }

    public void Initialize(AutomationType selectedAutomationType)
    {
        SelectedAutomationType = selectedAutomationType;
        IsInitialized = true;

        _automation = selectedAutomationType == AutomationType.UIA2 ? new UIA2Automation() : new UIA3Automation();
        _rootElement = _automation.GetDesktop();
        var desktopViewModel = new ElementViewModel(_rootElement, this);
        desktopViewModel.SelectionChanged += DesktopViewModel_SelectionChanged;
        desktopViewModel.LoadChildren(false);
        Elements.Add(desktopViewModel);
        Elements[0].IsExpanded = true;

        // Initialize TreeWalker
        _treeWalker = _automation.TreeWalkerFactory.GetControlViewWalker();

        // Initialize hover
        _hoverMode = new HoverMode(_automation);
        _hoverMode.ElementHovered += ElementToSelectChanged;

        // Initialize focus tracking
        _focusTrackingMode = new FocusTrackingMode(_automation);
        _focusTrackingMode.ElementFocused += ElementToSelectChanged;
    }

    private void ElementToSelectChanged(AutomationElement obj)
    {
        // Build a stack from the root to the hovered item
        Stack<AutomationElement> pathToRoot = new Stack<AutomationElement>();
        while (obj != null)
        {
            // Break on circular relationship (should not happen?)
            if (pathToRoot.Contains(obj) || obj.Equals(_rootElement))
            {
                break;
            }

            pathToRoot.Push(obj);
            try
            {
                obj = _treeWalker.GetParent(obj);
            }
            catch (Exception ex)
            {
                // TODO: Log
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        // Expand the root element if needed
        if (!Elements[0].IsExpanded)
        {
            Elements[0].IsExpanded = true;
            Thread.Sleep(1000);
        }

        ElementViewModel elementVm = Elements[0];
        while (pathToRoot.Count > 0)
        {
            AutomationElement elementOnPath = pathToRoot.Pop();
            ElementViewModel nextElementVm = FindElement(elementVm, elementOnPath);
            if (nextElementVm == null)
            {
                // Could not find next element, try reloading the parent
                elementVm.LoadChildren(true);
                // Now search again
                nextElementVm = FindElement(elementVm, elementOnPath);
                if (nextElementVm == null)
                {
                    // The next element is still not found, exit the loop
                    Console.WriteLine("Could not find the next element!");
                    break;
                }
            }
            elementVm = nextElementVm;
            if (!elementVm.IsExpanded)
            {
                elementVm.IsExpanded = true;
            }
        }
        // Select the last element
        elementVm.IsSelected = true;
    }

    private ElementViewModel FindElement(ElementViewModel parent, AutomationElement element)
    {
        return parent.Children.FirstOrDefault(child => child.AutomationElement.Equals(element));
    }

    private void DesktopViewModel_SelectionChanged(ElementViewModel obj)
    {
        SelectedItemInTree = obj;
        this.OnPropertyChanged(() => SelectedItemDetails);
    }

    private void RefreshTree()
    {
        Elements.Clear();
        Initialize(SelectedAutomationType);
    }
}