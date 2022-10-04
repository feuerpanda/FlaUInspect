using FlaUInspect.ViewModels;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace FlaUInspect.Views;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        this.InitializeComponent();
        this.AppendVersionToTitle();
        this.Height = 600;
        this.Width = 800;
        Loaded += this.MainWindow_Loaded;
        _vm = new MainViewModel();
        this.DataContext = _vm;
    }

    private void AppendVersionToTitle()
    {
        AssemblyInformationalVersionAttribute attr =
            Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
        if (attr != null)
        {
            this.Title += " v" + attr.InformationalVersion;
        }
    }

    private void MainWindow_Loaded(object sender, EventArgs e)
    {
        if (!_vm.IsInitialized)
        {
            ChooseVersionWindow dlg = new() { Owner = this };
            if (dlg.ShowDialog() != true)
            {
                this.Close();
            }
            _vm.Initialize(dlg.SelectedAutomationType);
            Loaded -= this.MainWindow_Loaded;
        }
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void TreeViewSelectedHandler(object sender, RoutedEventArgs e)
    {
        TreeViewItem item = sender as TreeViewItem;
        if (item != null)
        {
            item.BringIntoView();
            e.Handled = true;
        }
    }
}