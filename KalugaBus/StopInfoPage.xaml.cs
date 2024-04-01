using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KalugaBus.Models;

namespace KalugaBus;

public partial class StopInfoPage : ContentPage
{
    private readonly long[] _trackIds;
    public Stop Stop { get; set; }
    public ObservableCollection<StopInfo> StopInfos { get; set; } = [];

    public StopInfoPage(Stop stop, long[] trackIds)
    {
        _trackIds = trackIds;
        Stop = stop;
        
        InitializeComponent();

        BindingContext = this;
    }

    private async void StopInfoPage_OnLoaded(object? sender, EventArgs e)
    {
        
    }

    private async Task FetchInfo()
    {
        
    }
}