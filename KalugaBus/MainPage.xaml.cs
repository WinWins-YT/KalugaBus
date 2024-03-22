using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Font = Mapsui.Styles.Font;

namespace KalugaBus;

public partial class MainPage : ContentPage
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
        feature["number"] = 6;
        var style = new LabelStyle
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
        feature.Styles.Add(style);
        
        return new MemoryLayer
        {
            Name = "Points",
            IsMapInfoLayer = true,
            Features = [feature],
            Style = new VectorStyle
            {
                Fill = new Brush(Color.LightCoral), 
                Outline = null,
                MaxVisible = 30
            }
        };
    }

    private async void MainPage_OnLoaded(object? sender, EventArgs e)
    {
        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        var location = await Geolocation.GetLastKnownLocationAsync();
        
        MapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        
        if (location is not null)
            MapView.MyLocationLayer.UpdateMyLocation(new Position(location.Latitude, location.Longitude));
        
        MapView.Map.Home = map =>
        {
            var point = SphericalMercator.FromLonLat(36.2637, 54.5136).ToMPoint();
            map.CenterOnAndZoomTo(point, 25);
        };
        MapView.Map.Layers.Add(CreatePointLayer());
        MapView.Info += MapViewOnInfo;
        CreateLineLayer();
    }

    private async void MapViewOnInfo(object? sender, MapInfoEventArgs e)
    {
        if (e.MapInfo.Feature is not PointFeature)
            return;
        
        var number = (int)e.MapInfo.Feature["number"];
        await DisplayAlert("Alert", number.ToString(), "OK");
    }
}
