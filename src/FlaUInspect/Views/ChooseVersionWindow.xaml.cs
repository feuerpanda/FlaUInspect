﻿using FlaUI.Core;
using System.Windows;

namespace FlaUInspect.Views;

/// <summary>
///     Interaction logic for ChooseVersionWindow.xaml
/// </summary>
public partial class ChooseVersionWindow
{
    public ChooseVersionWindow()
    {
        this.InitializeComponent();
    }

    public AutomationType SelectedAutomationType { get; private set; }

    private void UIA2ButtonClick(object sender, RoutedEventArgs e)
    {
        this.SelectedAutomationType = AutomationType.UIA2;
        this.DialogResult = true;
    }

    private void UIA3ButtonClick(object sender, RoutedEventArgs e)
    {
        this.SelectedAutomationType = AutomationType.UIA3;
        this.DialogResult = true;
    }
}