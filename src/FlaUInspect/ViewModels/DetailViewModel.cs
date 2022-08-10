using FlaUI.Core;
using FlaUInspect.Core;

namespace FlaUInspect.ViewModels;

public interface IDetailViewModel
{
    string Key { get; set; }
    string Value { get; set; }
    bool Important { get; set; }
}

public class DetailViewModel : ObservableObject, IDetailViewModel
{
    public DetailViewModel(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key
    {
        get => this.GetProperty<string>();
        set => this.SetProperty(value);
    }

    public string Value
    {
        get => this.GetProperty<string>();
        set => this.SetProperty(value);
    }

    public bool Important
    {
        get => this.GetProperty<bool>();
        set => this.SetProperty(value);
    }

    public static DetailViewModel FromAutomationProperty<T>(string key, IAutomationProperty<T> value)
    {
        return new DetailViewModel(key, value.ToDisplayText());
    }
}