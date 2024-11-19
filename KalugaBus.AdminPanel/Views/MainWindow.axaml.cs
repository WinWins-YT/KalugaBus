using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;
using KalugaBus.AdminPanel.Models;
using KalugaBus.AdminPanel.Services;
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
    private readonly OptionsService<Settings> _settings;
    private List<TrackPolyline> _trackPolylines = [];
    
    public MainWindow(OptionsService<Settings> options)
    {
        InitializeComponent();
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        _settings = options;
    }
    
    public MainWindow() : this(new OptionsService<Settings>()) { }

    private void MainWindow_OnLoaded(object? sender, RoutedEventArgs e)
    {
        PointMapView.Map.Home = map =>
        {
            var point = SphericalMercator.FromLonLat(36.2754200, 54.5293000).ToMPoint();
            map.CenterOnAndZoomTo(point, 15);
        };
        
        PointMapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

        Task.Run(async () =>
        {
            await LoadRoutes();
            await LoadTracks();
        });
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
            Dispatcher.UIThread.Post(async () =>
            {
                var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "Error occurred",
                    ContentMessage = "Unable to get polylines\n" + ex,
                    ButtonDefinitions = ButtonEnum.Ok,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                });
                await msg.ShowAsPopupAsync(this);
            });
            
        }
    }

    private async Task LoadTracks()
    {
        var filePath = Path.Combine(_settings.Value.WorkingDirectory, "tracks.json");
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            var routeDevices = JsonSerializer.Deserialize<List<RouteDevice>>(json, _jsonSerializerOptions) ??
                             throw new InvalidOperationException("Wrong JSON in tracks.json");
            
            var pointsComboBoxItems = routeDevices.Select(x => $"{x.TrackId} - {x.Name}").ToList();
            Dispatcher.UIThread.Post(() =>
            {
                PointRouteComboBox.ItemsSource = pointsComboBoxItems;
                PointRouteComboBox.SelectedIndex = 0;
                PointRouteComboBox.IsEnabled = true;
            });
        }
        else
        {
            var pointsComboBoxItems = _trackPolylines.Select(x => $"ID {x.Id}").ToList();
            Dispatcher.UIThread.Post(async () =>
            {
                PointRouteComboBox.ItemsSource = pointsComboBoxItems;
                PointRouteComboBox.SelectedIndex = 0;
                PointRouteComboBox.IsEnabled = true;
                
                var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "No tracks file",
                    ContentMessage = "File tracks.json not found",
                    ButtonDefinitions = ButtonEnum.Ok,
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                });
                await msg.ShowAsPopupAsync(this);
            });
        }
    }
}