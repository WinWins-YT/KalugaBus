using KalugaBus.PointProviders;
using KalugaBus.StyleRenders;
using KalugaBus.Styles;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Layers.AnimatedLayers;
using Mapsui.Projections;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.UI.Maui;
using Mapsui.Widgets;
using Microsoft.Maui.Devices.Sensors;
using AnimatedPointLayer = KalugaBus.RefactoredMapsUi.Layers.AnimatedLayer.AnimatedPointLayer;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Font = Mapsui.Styles.Font;

namespace KalugaBus;

public partial class MainPage
{
    private readonly BusPointProvider _busPointProvider = new();
    private readonly BusStyle _busStyle = new();
    
    public MainPage()
    {
        InitializeComponent();
    }

    private void CreateLineLayer()
    {
        var polygon = new Polyline
        {
            Tag = "track",
            Positions =
            {
                new Position(54.515321, 36.248665),
                new Position(54.512669, 36.271408),
                new Position(54.499208, 36.280574)
            },
            StrokeColor = Colors.Red,
            StrokeWidth = 5f,
            MaxVisible = 30
        };
        
        MapView.Drawables.Add(polygon);
    }
    
    private static MemoryLayer CreatePointLayer()
    {
        var feature = new PointFeature(SphericalMercator.FromLonLat(36.240257, 54.514117).ToMPoint());
        feature["number"] = "18";
        feature["tag"] = "bus";
        feature["rotation"] = 45;
        /*var style = new LabelStyle
        {
            Text = "6",
            Font = new Font
            {
                Bold = true,
                FontFamily = "OpenSans",
            },
            MaxVisible = 30,
            ForeColor = Color.White,
            BackColor = new Brush(Color.Transparent)
        };
        feature.Styles.Add(style);*/
        //feature.Styles.Add(new BusStyle());
        
        return new MemoryLayer
        {
            Name = "Points",
            IsMapInfoLayer = true,
            Features = [feature],
            Style = null
            /*Style = new VectorStyle
            {
                Fill = new Brush(Color.LightCoral), 
                Outline = null,
                MaxVisible = 30
            }*/
        };
    }

    private void MainPage_OnLoaded(object? sender, EventArgs e)
    {
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
        
        if (MapView.Renderer is MapRenderer && !MapView.Renderer.StyleRenderers.ContainsKey(typeof(BusStyle)))
            MapView.Renderer.StyleRenderers.Add(typeof(BusStyle), new BusStyleRender());
        
        MapView.Info += MapViewOnInfo;
        MapView.MapClicked += MapViewOnMapClicked;

        Task.Run(UpdateLocation);
    }

    private void MapViewOnMapClicked(object? sender, MapClickedEventArgs e)
    {
        /*_busPointProvider.ShowTrackId = -1;
        
        MapView.Map.Layers.Remove(x => x.Name == "Points");
        MapView.Map.Layers.Add(new AnimatedPointLayer(_busPointProvider)
        {
            Name = "Points",
            IsMapInfoLayer = true,
            Style = new ThemeStyle(f => new BusStyle())
        });*/
    }

    private void MapViewOnInfo(object? sender, MapInfoEventArgs e)
    {
        var feature = e.MapInfo?.Feature;
        if (feature is not PointFeature busPoint || busPoint["tag"]?.ToString() != "bus")
        {
            _busPointProvider.ShowTrackId = -1;
            return;
        }

        /*MapView.Map.Layers.Remove(x => x.Name == "Points");
        MapView.Map.Layers.Add(new AnimatedPointLayer(_busPointProvider)
        {
            Name = "Points",
            IsMapInfoLayer = true,
            Style = new ThemeStyle(f => new BusStyle())
        });*/
        
        (MapView.Map.Layers[1] as AnimatedPointLayer)?.ClearCache();
        
        _busPointProvider.ShowTrackId = (long)(busPoint["track_id"] ?? -1);
    }

    private async Task UpdateLocation()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        while (true)
        {
            Location? location = null;
            try
            {
                location = await Geolocation.GetLastKnownLocationAsync();
            }
            catch (InvalidOperationException)
            {
                await DisplayAlert("Местоположение недоступно",
                    "Это приложение требует доступа к местоположению. Включите местоположение в настройках", "OK");
                Application.Current?.Quit();
            }
            
            if (location is not null)
                MapView.MyLocationLayer.UpdateMyLocation(new Position(location.Latitude, location.Longitude));

            await timer.WaitForNextTickAsync();
        }
    }
}
