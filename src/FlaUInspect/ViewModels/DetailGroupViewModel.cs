using FlaUInspect.Core;
using System.Collections.Generic;

namespace FlaUInspect.ViewModels;

public class DetailGroupViewModel : ObservableObject
{
    public DetailGroupViewModel(string name, IEnumerable<IDetailViewModel> details)
    {
        Name = name;
        Details = new ExtendedObservableCollection<IDetailViewModel>(details);
    }

    public string Name
    {
        get => this.GetProperty<string>();
        set => this.SetProperty(value);
    }

    public ExtendedObservableCollection<IDetailViewModel> Details { get; set; }
}