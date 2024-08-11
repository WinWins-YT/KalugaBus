using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using KalugaBus.Models;
using KalugaBus.PointProviders;
using KalugaBus.StyleRenderers;
using KalugaBus.Styles;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Rendering.Skia;
using Mapsui.Styles.Thematics;
using Mapsui.UI.Maui;
using Mapsui.Widgets.Zoom;
using AnimatedPointLayer = KalugaBus.RefactoredMapsUi.Layers.AnimatedLayer.AnimatedPointLayer;
using HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment;
using VerticalAlignment = Mapsui.Widgets.VerticalAlignment;

namespace KalugaBus;

public partial class MainPage : IQueryAttributable
{
    private readonly BusPointProvider _busPointProvider = new();
    private readonly BusStyle _busStyle = new();
    private readonly BusStyleRenderer _busStyleRenderer = new();

    private readonly StationPointProvider _stationPointProvider = new();
    private readonly StationStyle _stationStyle = new();
    private readonly StationStyleRenderer _stationStyleRenderer = new();

    private readonly MemoryLayer _stationsLayer = new();
    
    public MainPage()
    {
        InitializeComponent();
    }

    private void MainPage_OnLoaded(object? sender, EventArgs e)
    {
        if (MapView.Map.Layers.Any(x => x.Name == "Points"))
            return;

        MapView.Map.Home = map =>
        {
            var point = SphericalMercator.FromLonLat(36.2754200, 54.5293000).ToMPoint();
            map.CenterOnAndZoomTo(point, 15);
        };

        MapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        
        _stationsLayer.Name = "Stations";
        _stationsLayer.IsMapInfoLayer = true;
        _stationsLayer.Style = new ThemeStyle(_ => _stationStyle);
        _stationPointProvider.DataChanged += async (_, _) =>
        {
            _stationsLayer.Features = await _stationPointProvider.GetFeaturesAsync(null!);
        };
        _stationPointProvider.DataHasChanged();
        MapView.Map.Layers.Add(_stationsLayer);
        
        MapView.Map.Layers.Add(new AnimatedPointLayer(_busPointProvider)
        {
            Name = "Buses",
            IsMapInfoLayer = true,
            Style = new ThemeStyle(_ => _busStyle)
        });
        
        MapView.Map.Widgets.Add(new ZoomInOutWidget
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            MarginX = 20,
            MarginY = 20,
        });
        
        if (MapView.Renderer is MapRenderer && !MapView.Renderer.StyleRenderers.ContainsKey(typeof(BusStyle)))
            MapView.Renderer.StyleRenderers.Add(typeof(BusStyle), _busStyleRenderer);
        
        if (MapView.Renderer is MapRenderer && !MapView.Renderer.StyleRenderers.ContainsKey(typeof(StationStyle)))
            MapView.Renderer.StyleRenderers.Add(typeof(StationStyle), _stationStyleRenderer);
        
        MapView.Info += MapViewOnInfo;
        
        Task.Run(UpdateLocation);
    }

    private async void MapViewOnInfo(object? sender, MapInfoEventArgs e)
    {
        var feature = e.MapInfo?.Feature;
        if (feature is null)
        {
            (MapView.Map.Layers[2] as AnimatedPointLayer)?.ClearCache();
            _busPointProvider.ShowTrackId = -1;
            _stationPointProvider.ShowTrackId = -1;
            return;
        }
        
        switch (feature["tag"]?.ToString())
        {
            case "bus":
                (MapView.Map.Layers[2] as AnimatedPointLayer)?.ClearCache();
                if (feature is not PointFeature busPoint)
                    return;

                _busPointProvider.ShowTrackId = (long)(busPoint["track_id"] ?? -1);
                _stationPointProvider.ShowTrackId = (long)(busPoint["track_id"] ?? -1);
                break;
            
            case "station":
                if (feature["station"] is not Station station)
                    return;
                var stop = new Stop {Station = station};
                await Navigation.PushAsync(new StopInfoPage(stop, stop.Station.TrackIds.ToArray()));
                break;
            
            default:
                (MapView.Map.Layers[2] as AnimatedPointLayer)?.ClearCache();
                _busPointProvider.ShowTrackId = -1;
                _stationPointProvider.ShowTrackId = -1;
                break;
        }
    }

    private async Task UpdateLocation()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var disclaimerShown = Preferences.Get("disclaimer_shown", false);
            if (!disclaimerShown)
            {
                var answer = await DisplayAlert("Дисклеймер",
                    "Данное приложение не связано с государственными органами и не несет ответственности за информацию, которую выводит. Данные берутся с сайта https://bus40.su",
                    "Политика конфиденциальности", "OK");
                if (answer)
                {
                    await Browser.Default.OpenAsync("https://danimatcorp.com/bus40/privacy.html", BrowserLaunchMode.SystemPreferred);
                }
                
                Preferences.Set("disclaimer_shown", true);
            }
        });
        
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        var permission = await CheckAndRequestLocationPermission();
        if (permission != PermissionStatus.Granted)
            return;
        
        while (true)
        {
            Location? location;
            try
            {
                location = await Geolocation.GetLocationAsync();
            }
            catch (InvalidOperationException)
            {
                await DisplayAlert("Местоположение недоступно",
                    "Это приложение требует доступа к местоположению. Включите местоположение в настройках", "OK");
                break;
            }
            
            if (location is not null)
                MapView.MyLocationLayer.UpdateMyLocation(new Position(location.Latitude, location.Longitude));

            await timer.WaitForNextTickAsync();
        }
    }
    
    private async Task<PermissionStatus> CheckAndRequestLocationPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status == PermissionStatus.Granted)
            return status;

        if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
        {
            await DisplayAlert("Местоположение недоступно",
                "Для отображения местоположения на карте, необходимо разрешить местоположение. Включите его в настройках приложения", "ОК");
            
            return status;
        }

        if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
        {
            await DisplayAlert("Местоположение недоступно",
                "Для отображения местоположения на карте, необходимо разрешить местоположение", "ОК");
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        });

        return status;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        (MapView.Map.Layers[2] as AnimatedPointLayer)?.ClearCache();
        if (query.TryGetValue("TrackId", out var value))
        {
            _busPointProvider.ShowTrackId = Convert.ToInt64(value);
            _stationPointProvider.ShowTrackId = Convert.ToInt64(value);
        }

        if (query.TryGetValue("ShowFavoured", out value))
        {
            _busPointProvider.ShowFavoured = value.ToString() == "1";
        }
    }

    private async void AboutMenuItem_OnClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AboutPage());
    }

    private void ShowStationsItem_OnClicked(object? sender, EventArgs e)
    {
        _stationPointProvider.ShowStations = !_stationPointProvider.ShowStations;
        
        ShowStationsItem.Text = _stationPointProvider.ShowStations
            ? "Скрыть остановки"
            : "Показать остановки";
    }
}
