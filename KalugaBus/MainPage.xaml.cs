using KalugaBus.PointProviders;
using KalugaBus.StyleRenders;
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
    private readonly BusStyleRender _busStyleRender = new();
    
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
        
        //MapView.Map.Layers.Add(CreatePointLayer());
        MapView.Map.Layers.Add(new AnimatedPointLayer(_busPointProvider)
        {
            Name = "Points",
            IsMapInfoLayer = true,
            Style = new ThemeStyle(f => _busStyle)
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
            MapView.Renderer.StyleRenderers.Add(typeof(BusStyle), _busStyleRender);
        
        MapView.Info += MapViewOnInfo;

        Task.Run(UpdateLocation);
    }

    private void MapViewOnInfo(object? sender, MapInfoEventArgs e)
    {
        var feature = e.MapInfo?.Feature;
        if (feature is not PointFeature busPoint || busPoint["tag"]?.ToString() != "bus")
        {
            (MapView.Map.Layers[1] as AnimatedPointLayer)?.ClearCache();
            _busPointProvider.ShowTrackId = -1;
            return;
        }
        
        (MapView.Map.Layers[1] as AnimatedPointLayer)?.ClearCache();
        
        _busPointProvider.ShowTrackId = (long)(busPoint["track_id"] ?? -1);
    }

    private async Task UpdateLocation()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        var permission = await CheckAndRequestLocationPermission();
        if (permission != PermissionStatus.Granted)
            return;
        
        while (true)
        {
            Location? location = null;
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
        (MapView.Map.Layers[1] as AnimatedPointLayer)?.ClearCache();
        _busPointProvider.ShowTrackId = Convert.ToInt64(query["TrackId"]);
    }
}
