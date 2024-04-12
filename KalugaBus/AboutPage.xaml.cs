using System.Windows.Input;

namespace KalugaBus;

public partial class AboutPage : ContentPage
{
    public string AppName => AppInfo.Current.Name;
    public string Version => $"v. {AppInfo.Current.VersionString}";
    
    public ICommand OpenLinkCommand => new Command<string>(async url => await Browser.OpenAsync(url));
    public AboutPage()
    {
        InitializeComponent();

        BindingContext = this;
    }
}