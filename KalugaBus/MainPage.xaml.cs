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
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Font = Mapsui.Styles.Font;

namespace KalugaBus;

public partial class MainPage
{
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

    private async void MainPage_OnLoaded(object? sender, EventArgs e)
    {
        MapView.Map.Home = map =>
        {
            var point = SphericalMercator.FromLonLat(36.2754200, 54.5293000).ToMPoint();
            map.CenterOnAndZoomTo(point, 15);
        };
        
        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

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

        MapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        
        if (location is not null)
            MapView.MyLocationLayer.UpdateMyLocation(new Position(location.Latitude, location.Longitude));
        
        //MapView.Map.Layers.Add(CreatePointLayer());
        MapView.Map.Layers.Add(new AnimatedPointLayer(new BusPointProvider())
        {
            Name = "Points",
            IsMapInfoLayer = true,
            Style = new ThemeStyle(f => new BusStyle())
        });
        
        if (MapView.Renderer is MapRenderer && !MapView.Renderer.StyleRenderers.ContainsKey(typeof(BusStyle)))
            MapView.Renderer.StyleRenderers.Add(typeof(BusStyle), new BusStyleRender());
        
        MapView.Info += MapViewOnInfo;
        //CreateLineLayer();
    }

    private async void MapViewOnInfo(object? sender, MapInfoEventArgs e)
    {
        
    }
}
