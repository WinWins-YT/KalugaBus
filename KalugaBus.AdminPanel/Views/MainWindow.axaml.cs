using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using KalugaBus.AdminPanel.Models;
using KalugaBus.AdminPanel.Services;
using KalugaBus.AdminPanel.StyleRenderers;
using KalugaBus.AdminPanel.ViewModels;
using KalugaBus.AdminPanel.Styles;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Editing;
using Mapsui.Nts.Extensions;
using Mapsui.Nts.Layers;
using Mapsui.Nts.Widgets;
using Mapsui.Projections;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.UI.Avalonia.Extensions;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using NetTopologySuite.Geometries;

namespace KalugaBus.AdminPanel.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly HttpClient _httpClient = new();
    private readonly OptionsService<Settings> _settings;
    private List<TrackPolyline> _trackPolylines = [];
    private List<RouteDevice> _routeDevices = [];
    private List<TrackStations> _trackStations = [];
    private readonly List<Station> _stations = new();
    
    private readonly StationStyle _stationStyle = new();
    private readonly StationStyleRenderer _stationStyleRenderer = new();

    private readonly VectorStyle _directLineStyle;
    private readonly VectorStyle _backLineStyle;

    private EditManager _pointEditManager = new();
    private WritableLayer _pointLayer = new();

    private bool _stopEditing = false;
    
    public MainWindow(OptionsService<Settings> options)
    {
        InitializeComponent();
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        _settings = options;
        
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
    
    public MainWindow() : this(new OptionsService<Settings>()) { }

    private async void MainWindow_OnLoaded(object? sender, RoutedEventArgs e)
    {
        PointMapView.Map.Home = map =>
        {
            var point = SphericalMercator.FromLonLat(36.2754200, 54.5293000).ToMPoint();
            map.CenterOnAndZoomTo(point, 15);
        };
        
        PointMapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        
        StopsMapView.Map.Home = map =>
        {
            var point = SphericalMercator.FromLonLat(36.2754200, 54.5293000).ToMPoint();
            map.CenterOnAndZoomTo(point, 15);
        };
        
        StopsMapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        
        if (StopsMapView.Renderer is MapRenderer && !StopsMapView.Renderer.StyleRenderers.ContainsKey(typeof(StationStyle)))
            StopsMapView.Renderer.StyleRenderers.Add(typeof(StationStyle), _stationStyleRenderer);

        _pointLayer = new WritableLayer
        {
            Name = "EditPoints",
            Style = CreateEditLayerStyle(),
            IsMapInfoLayer = true
        };
        PointMapView.Map.Layers.Add(_pointLayer);

        _pointEditManager = new EditManager
        {
            Layer = (WritableLayer)PointMapView.Map.Layers.First(x => x.Name == "EditPoints"),
            EditMode = EditMode.Modify
        };
        
        PointMapView.Map.Widgets.Add(new EditingWidget(PointMapView, _pointEditManager, new EditManipulation()));
        PointMapView.Map.Layers.Add(new VertexOnlyLayer(_pointLayer) { Name = "VertexLayer" });
        
        var stationsLayer = new MemoryLayer();
        stationsLayer.Name = "Stations";
        stationsLayer.IsMapInfoLayer = true;
        stationsLayer.Style = new ThemeStyle(_ => _stationStyle);
        StopsMapView.Map.Layers.Add(stationsLayer);
        
        await LoadStops();
        await LoadRoutes();
        await LoadTracks();
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
            Dispatcher.UIThread.Post(() =>
            {
                var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "Error occurred",
                    ContentMessage = "Unable to get polylines\n" + ex,
                    ButtonDefinitions = ButtonEnum.Ok,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                });
                msg.ShowAsPopupAsync(this);
            });
        }
    }

    private async Task LoadTracks()
    {
        var filePath = Path.Combine(_settings.Value.WorkingDirectory, "tracks.json");
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            _routeDevices = JsonSerializer.Deserialize<List<RouteDevice>>(json, _jsonSerializerOptions) ??
                             throw new InvalidOperationException("Wrong JSON in tracks.json");

            _routeDevices = _routeDevices.Select(x =>
            {
                x.ImageUrl = "avares://KalugaBus.AdminPanel/Assets/" + x.ImageUrl;
                return x;
            }).ToList();
            
            Dispatcher.UIThread.Post(() =>
            {
                var pointsComboBoxItems = _routeDevices
                    .Select(x => new ComboBoxItem { Content = $"{x.TrackId} - {x.Name}", Tag = x.TrackId }).ToList();
                PointRouteComboBox.ItemsSource = pointsComboBoxItems;
                PointRouteComboBox.SelectedIndex = 0;
                PointRouteComboBox.IsEnabled = true;
                
                StopsRouteComboBox.ItemsSource = pointsComboBoxItems;
                StopsRouteComboBox.SelectedIndex = 0;
                StopsRouteComboBox.IsEnabled = true;

                RouteListBox.ItemsSource = _routeDevices;
            });
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                var pointsComboBoxItems = _trackPolylines
                    .Select(x => new ComboBoxItem { Content = $"ID {x.Id}", Tag = x.Id}).ToList();
                PointRouteComboBox.ItemsSource = pointsComboBoxItems;
                PointRouteComboBox.SelectedIndex = 0;
                PointRouteComboBox.IsEnabled = true;
                
                StopsRouteComboBox.ItemsSource = pointsComboBoxItems;
                StopsRouteComboBox.SelectedIndex = 0;
                StopsRouteComboBox.IsEnabled = true;
                
                var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "No tracks file",
                    ContentMessage = "File tracks.json not found",
                    ButtonDefinitions = ButtonEnum.Ok,
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                });
                msg.ShowAsPopupAsync(this);
            });
        }
    }
    
    private async Task LoadStops()
    {
        try
        {
            var trackStationsJson =
                await _httpClient.GetStringAsync("https://bus40.su/default.aspx?target=main&action=get_stations");
            _trackStations =
                JsonSerializer.Deserialize<List<TrackStations>>(trackStationsJson, _jsonSerializerOptions) ??
                throw new InvalidOperationException("Wrong JSON was received from get_polylines");
            foreach (var trackStation in _trackStations)
            {
                foreach (var station in trackStation.Stations.DistinctBy(x => x.Id))
                {
                    if (_stations.All(x => x.Name != station.Name))
                        _stations.Add(station);

                    _stations.First(x => x.Name == station.Name).TrackIds.Add(trackStation.Id);
                }
            }
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "Error occurred",
                    ContentMessage = "Unable to get polylines\n" + ex,
                    ButtonDefinitions = ButtonEnum.Ok,
                    Icon = MsBox.Avalonia.Enums.Icon.Error
                });
                msg.ShowAsPopupAsync(this);
            });
        }
    }

    private void PointRouteComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PointRouteComboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;
        
        var trackId = (long)selectedItem.Tag!;
        var track = _trackPolylines.First(x => x.Id == trackId);
        
        var directPoints = track.Data.Direct
            .Select(x => SphericalMercator.FromLonLat(x[1], x[0]).ToCoordinate()).ToArray();
        var backPoints = track.Data.Back
            .Select(x => SphericalMercator.FromLonLat(x[1], x[0]).ToCoordinate()).ToArray();
        
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
        
        _pointLayer.Clear();
        _pointLayer.AddRange([directGeometry, backGeometry]);
        _pointLayer.DataHasChanged();
    }

    #region Styles

    private StyleCollection CreateEditLayerStyle() => new()
    {
        Styles =
        {
            CreateEditLayerBasicStyle(),
            CreateSelectedStyle(),
            CreateStyleToShowTheVertices(),
        }
    };

    private SymbolStyle CreateStyleToShowTheVertices() => new() { SymbolScale = 0.5 };

    private VectorStyle CreateEditLayerBasicStyle() => new()
    {
        Fill = new Brush(_editModeColor),
        Line = new Pen(_editModeColor, 3),
        Outline = new Pen(_editModeColor, 3)
    };

    private readonly Color _editModeColor = new(124, 22, 111, 180);
    private readonly Color _pointLayerColor = new(240, 240, 240, 240);
    private readonly Color _lineLayerColor = new(150, 150, 150, 240);
    private readonly Color _polygonLayerColor = new(20, 20, 20, 240);

    private readonly SymbolStyle? _selectedStyle = new()
    {
        Fill = null,
        Outline = new Pen(Color.Red, 3),
        Line = new Pen(Color.Red, 3)
    };

    private readonly SymbolStyle? _disableStyle = new() { Enabled = false };
    
    private ThemeStyle CreateSelectedStyle()
        => new(f => (bool?)f["Selected"] == true ? _selectedStyle : _disableStyle);

    #endregion

    private void StopsRouteComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (StopsRouteComboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;
        
        var trackId = (long)selectedItem.Tag!;
        var track = _trackStations.First(x => x.Id == trackId);
        var stops = track.Stations;
        
        var features = new List<IFeature>();
        var ids = new List<long>();
        foreach (var station in stops)
        {
            if (ids.Contains(station.Id))
                continue;
            var feature = new PointFeature(SphericalMercator
                .FromLonLat(station.Longitude, station.Latitude).ToMPoint());

            feature["tag"] = "station";
            feature["ID"] = station.Id;
            feature["station"] = _stations.First(x => x.Name == station.Name);
            feature["editing"] = false;

            ids.Add(station.Id);
            features.Add(feature);
        }
        
        var layer = (MemoryLayer)StopsMapView.Map.Layers.First(x => x.Name == "Stations");
        layer.Features = features;
    }

    private void StopsMapView_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Control);
        if (point.Properties.IsRightButtonPressed)
        {
            if (_stopEditing) return;
            var location = point.Position.ToMapsui();
            var mapInfo = StopsMapView.GetMapInfo(location);
            if (mapInfo?.Feature == null)
                return;
    
            var layer = (MemoryLayer)StopsMapView.Map.Layers.First(x => x.Name == "Stations");
            var stop = layer.Features.First(x => x["ID"] == mapInfo.Feature["ID"]);
            stop["editing"] = true;
            _stopEditing = true;
            StopsMapView.Refresh();
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            var mapInfo = StopsMapView.GetMapInfo(point.Position.ToMapsui());

            if (_stopEditing)
            {
                var viewport = StopsMapView.Map.Navigator.Viewport;
                var location = viewport.ScreenToWorld(point.Position.ToMapsui());

                var layer = (MemoryLayer)StopsMapView.Map.Layers.First(x => x.Name == "Stations");
                var features = layer.Features.ToList();
                for (var i = 0; i < features.Count; i++)
                {
                    if (!(bool)(features[i]["editing"] ?? false))
                        continue;

                    var newStop = new PointFeature(location);
                    newStop["tag"] = "station";
                    newStop["ID"] = features[i]["ID"];
                    newStop["station"] = features[i]["station"];
                    newStop["editing"] = false;

                    features[i] = newStop;
                }

                layer.Features = features;
                _stopEditing = false;
                StopsMapView.Refresh();
            }
            else
            {
                if (mapInfo?.Feature == null)
                    return;

                var station = mapInfo.Feature["station"] as Station ?? throw new InvalidOperationException();
                var stopEdit = new StopEdit(station, _routeDevices.Where(x => station.TrackIds.Contains(x.TrackId)));
                stopEdit.ShowDialog(this);
            }
        }
        
        e.Handled = true;
    }

    private void StopsMapView_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !_stopEditing) 
            return;
        
        var layer = (MemoryLayer)StopsMapView.Map.Layers.First(x => x.Name == "Stations");
        var stop = layer.Features.First(x => (bool)(x["editing"] ?? false));
        stop["editing"] = false;
        _stopEditing = false;
        StopsMapView.Refresh();
    }

    private void RouteListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RouteListBox.SelectedItem is not RouteDevice selectedItem)
            return;

        var routeEdit = new RouteEdit(selectedItem);
        routeEdit.ShowDialog(this);
    }
}