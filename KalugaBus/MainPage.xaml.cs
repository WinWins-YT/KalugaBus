using System.Text;
using KalugaBus.Enums;
using KalugaBus.Models;
using KalugaBus.PointProviders;
using KalugaBus.StyleRenderers;
using KalugaBus.Styles;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.Widgets;
using Mapsui.Widgets.ButtonWidgets;
using Mapsui.Widgets.InfoWidgets;
using NetTopologySuite.Geometries;
using AnimatedPointLayer = KalugaBus.RefactoredMapsUi.Layers.AnimatedLayer.AnimatedPointLayer;
using Color = Mapsui.Styles.Color;
using HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment;
using Location = Microsoft.Maui.Devices.Sensors.Location;
using Position = Mapsui.UI.Maui.Position;
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

    private readonly MemoryLayer _lineLayer = new();
    private readonly VectorStyle _directLineStyle;
    private readonly VectorStyle _backLineStyle;
    
    public MainPage()
    {
        InitializeComponent();

        _directLineStyle = new VectorStyle
        {
            Fill = null,
            Outline = null,
            Line = new Pen { Color = Color.FromArgb(200, 51, 45, 237), Width = 5 },
            MaxVisible = 30
        };
        _backLineStyle = new VectorStyle
        {
            Fill = null,
            Outline = null,
            Line = new Pen { Color = Color.FromArgb(200, 237, 55, 45), Width = 5 },
            MaxVisible = 30
        };
    }

    private void MainPage_OnLoaded(object? sender, EventArgs e)
    {
        if (MapView.Map.Layers.Any(x => x.Name == "Buses"))
            return;

        MapView.Map.Navigator.CenterOnAndZoomTo(SphericalMercator.FromLonLat(36.2754200, 54.5293000).ToMPoint(), 15);

        MapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

        var stationsLayer = new MemoryLayer
        {
            Name = "Stations",
            Style = new ThemeStyle(_ => _stationStyle)
        };
        _stationPointProvider.DataChanged += async (_, _) =>
        {
            stationsLayer.Features = await _stationPointProvider.GetFeaturesAsync(null!);
        };
        _stationPointProvider.DataHasChanged();
        MapView.Map.Layers.Add(stationsLayer);
        
        MapView.Map.Layers.Add(_lineLayer);
        
        MapView.Map.Layers.Add(new AnimatedPointLayer(_busPointProvider)
        {
            Name = "Buses",
            Style = new ThemeStyle(_ => _busStyle)
        });
        
        MapView.Map.Widgets.Add(new ZoomInOutWidget
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new MRect(20)
        });
        
        var infoWidget = new MapInfoWidget(MapView.Map, x => x.Name == "Buses")
        {
            FeatureToText = feature =>
            {
                if (feature is not PointFeature pointFeature || pointFeature["tag"]?.ToString() != "bus")
                    return string.Empty;

                var output = new StringBuilder();
                var trackType = (TrackType)(pointFeature["track_type"] ?? 0);

                output.Append(trackType == TrackType.Bus ? "Автобус №" : "Троллейбус №");
                output.AppendLine(pointFeature["number"]?.ToString() ?? string.Empty);
                output.AppendLine($"Номер: {pointFeature["bus_number"]?.ToString() ?? string.Empty}");
                output.AppendLine($"Скорость: {pointFeature["speed"]?.ToString() ?? string.Empty} км/ч");
                output.AppendLine();
                output.AppendLine($"Машин на линии: {pointFeature["bus_count"]?.ToString() ?? string.Empty}");

                return output.ToString();
            }
        };
        MapView.Map.Widgets.Add(infoWidget);
        
        MapRenderer.RegisterStyleRenderer(typeof(BusStyle), _busStyleRenderer);
        MapRenderer.RegisterStyleRenderer(typeof(StationStyle), _stationStyleRenderer);
        
        MapView.Info += MapViewOnInfo;
        
        Task.Run(UpdateLocation);
    }

    private async void MapViewOnInfo(object? sender, MapInfoEventArgs e)
    {
        var feature = e.GetMapInfo(MapView.Map.Layers.Where(x => x.Name == "Buses")).Feature;
        if (feature is null)
        {
            (MapView.Map.Layers.First(x => x.Name == "Buses") as AnimatedPointLayer)?.ClearCache();
            _busPointProvider.ShowTrackId = -1;
            _stationPointProvider.ShowTrackId = -1;
            ClearBusRoute();
            return;
        }
        
        switch (feature["tag"]?.ToString())
        {
            case "bus":
                (MapView.Map.Layers.First(x => x.Name == "Buses") as AnimatedPointLayer)?.ClearCache();
                if (feature is not PointFeature busPoint)
                    return;

                _busPointProvider.ShowTrackId = (long)(busPoint["track_id"] ?? -1);
                _stationPointProvider.ShowTrackId = (long)(busPoint["track_id"] ?? -1);
                ShowBusRoute((long)(busPoint["track_id"] ?? -1));
                break;
            
            case "station":
                if (feature["station"] is not Station station)
                    return;
                var stop = new Stop {Station = station};
                await Navigation.PushAsync(new StopInfoPage(stop, stop.Station.TrackIds.ToArray()));
                break;
            
            default:
                (MapView.Map.Layers.First(x => x.Name == "Buses") as AnimatedPointLayer)?.ClearCache();
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
        (MapView.Map.Layers.First(x => x.Name == "Buses") as AnimatedPointLayer)?.ClearCache();
        if (query.TryGetValue("TrackId", out var value))
        {
            _busPointProvider.ShowTrackId = Convert.ToInt64(value);
            _stationPointProvider.ShowTrackId = Convert.ToInt64(value);
            ShowBusRoute(Convert.ToInt64(value));
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

    private void ShowBusRoute(long id)
    {
        var trackPolyline = _busPointProvider.TrackPolylines.FirstOrDefault(x => x.Id == id);
        if (trackPolyline is null)
        {
            ClearBusRoute();
            return;
        }

        var directPoints = trackPolyline.Data.Direct.Select(x => SphericalMercator.FromLonLat(x[1], x[0]).ToCoordinate())
            .ToArray();
        var backPoints = trackPolyline.Data.Back.Select(x => SphericalMercator.FromLonLat(x[1], x[0]).ToCoordinate())
            .ToArray();

        var lineDirect = new LineString(directPoints);
        var lineBack = new LineString(backPoints);

        var directGeometry = new GeometryFeature(lineDirect)
        {
            Styles = [_directLineStyle]
        };
        var backGeometry = new GeometryFeature(lineBack)
        {
            Styles = [_backLineStyle]
        };

        _lineLayer.Features = [directGeometry, backGeometry];
        _lineLayer.DataHasChanged();
    }

    private void ClearBusRoute()
    {
        _lineLayer.Features = [];
        _lineLayer.DataHasChanged();
    }
}
