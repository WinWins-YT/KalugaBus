using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using KalugaBus.AdminPanel.Models;

namespace KalugaBus.AdminPanel.Views;

public partial class StopEdit : Window
{
    private Station _station;

    public ObservableCollection<RouteDevice> RouteDevices { get; set; }
    public string StationName { get; set; }
    public StopEdit(Station station, IEnumerable<RouteDevice>? routeDevices = null)
    {
        DataContext = this;
        
        _station = station;
        StationName = station.Name;
        RouteDevices = new ObservableCollection<RouteDevice>(routeDevices ?? 
                                                             station.TrackIds.Select(x => new RouteDevice { TrackId = x, Name = x.ToString() }));
        
        InitializeComponent();
    }
}