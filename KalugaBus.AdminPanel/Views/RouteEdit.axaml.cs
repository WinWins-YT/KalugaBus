using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KalugaBus.AdminPanel.Models;

namespace KalugaBus.AdminPanel.Views;

public partial class RouteEdit : Window
{
    private readonly RouteDevice _routeDevice;
    
    public string RouteName { get; set; }
    public string Route { get; set; }
    public int RouteType { get; set; }

    public RouteEdit(RouteDevice routeDevice)
    {
        DataContext = this;
        _routeDevice = routeDevice;
        RouteName = routeDevice.Name;
        Route = routeDevice.Route;
        RouteType = routeDevice.ImageUrl.Contains("trolleybus") ? 1 : 0;
        InitializeComponent();
    }
}