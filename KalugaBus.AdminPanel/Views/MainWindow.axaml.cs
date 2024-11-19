using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using KalugaBus.AdminPanel.Models;
using KalugaBus.AdminPanel.ViewModels;
using Mapsui.Extensions;
using Mapsui.Projections;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace KalugaBus.AdminPanel.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly HttpClient _httpClient = new();
    private List<TrackPolyline> _trackPolylines = [];
    
    public MainWindow()
    {
        InitializeComponent();
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    private void MainWindow_OnLoaded(object? sender, RoutedEventArgs e)
    {
        PointMapView.Map.Home = map =>
        {
            var point = SphericalMercator.FromLonLat(36.2754200, 54.5293000).ToMPoint();
            map.CenterOnAndZoomTo(point, 15);
        };
        
        PointMapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

        Task.Run(LoadRoutes).Wait();
        
        
    }

    private async Task LoadRoutes()
    {
        try
        {
            var trackPolylineJson =
                await _httpClient.GetStringAsync("https://bus40.su/default.aspx?target=main&action=get_polylines");
            _trackPolylines =
                JsonSerializer.Deserialize<List<TrackPolyline>>(trackPolylineJson, _jsonSerializerOptions) ??
                throw new InvalidOperationException("Wrong JSON was received from get_polylines");
        }
        catch (Exception ex)
        {
            var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = "Error occurred",
                ContentMessage = "Unable to get polylines\n" + ex,
                ButtonDefinitions = ButtonEnum.Ok,
                Icon = MsBox.Avalonia.Enums.Icon.Error
            });
            await msg.ShowAsPopupAsync(this);
        }
    }
}