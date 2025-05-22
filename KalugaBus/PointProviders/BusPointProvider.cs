using System.Text.Encodings.Web;
using System.Text.Json;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using KalugaBus.Extensions;
using KalugaBus.Models;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Device = KalugaBus.Models.Device;

namespace KalugaBus.PointProviders;

public class BusPointProvider : MemoryProvider, IDynamic, IDisposable
{
    private readonly CancellationTokenSource _tokenSource = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(3));
    private readonly HttpClient _httpClient = new();
    private List<Device> _devices = [];
    public List<TrackPolyline> TrackPolylines = [];
    private readonly Dictionary<long, long> _incomingIds = new();
    private long _showTrackId = -1;
    private bool _showFavoured;
    private bool _showTrackIdSet;
    private bool _showedError;
    private bool _showedNoBuses;

    public long ShowTrackId
    {
        get => _showTrackId;
        set
        {
            _showTrackId = value;
            _incomingIds.Clear();
            _showTrackIdSet = true;
            _showedNoBuses = false;
            ShowFavoured = false;
            OnDataChanged();
        }
    }

    public bool ShowFavoured
    {
        get => _showFavoured;
        set
        {
            _showFavoured = value;
            _incomingIds.Clear();
            OnDataChanged();
        }
    }

    public BusPointProvider()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        Catch.TaskRun(FetchBusDataAsync);
    }

    private readonly Dictionary<long, MPoint> _previousPoints = new();
    private readonly Dictionary<long, double> _previousRotations = new();
    
    private async Task FetchBusDataAsync()
    {
        await LoadInfo();
        
        while (!_tokenSource.IsCancellationRequested)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(
                    "https://bus40.su/default.aspx?target=main&action=get_devices");
                _devices = JsonSerializer.Deserialize<List<Device>>(json, _jsonSerializerOptions) ??
                           throw new InvalidOperationException("Wrong JSON was received from get_devices");
            }
            catch (Exception ex)
            {
                if (!_showedError)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var toast = Toast.Make($"Произошла ошибка:\n{ex.Message}", ToastDuration.Long);
                        await toast.Show();
                    });
                    _showedError = true;
                }
            }

            foreach (var device in _devices.Where(device => !_previousPoints.ContainsKey(device.Id)))
            {
                _previousPoints[device.Id] = new MPoint();
            }

            OnDataChanged();
            
            await _timer.WaitForNextTickAsync(_tokenSource.Token);
        }
    }

    private async Task LoadInfo()
    {
        try
        {
            var trackPolylineJson =
                await _httpClient.GetStringAsync("https://bus40.su/default.aspx?target=main&action=get_polylines");
            TrackPolylines =
                JsonSerializer.Deserialize<List<TrackPolyline>>(trackPolylineJson, _jsonSerializerOptions) ??
                throw new InvalidOperationException("Wrong JSON was received from get_polylines");
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var toast = Toast.Make($"Произошла ошибка:\n{ex.Message}", ToastDuration.Long);
                await toast.Show();
            });
        }
    }

    public override async Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
    {
        List<IFeature> points = [];
        List<Device> devices;
        if (ShowFavoured)
        {
            var favouredTracksJson = Preferences.Get("favoured_tracks", "");
            if (!string.IsNullOrEmpty(favouredTracksJson))
            {
                var favouredTracks =
                    JsonSerializer.Deserialize<List<long>>(favouredTracksJson, _jsonSerializerOptions) ??
                    throw new InvalidOperationException("Wrong JSON was received from favoured_tracks");
                
                devices = _devices.Where(x => favouredTracks.Contains(x.TrackId)).ToList();
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var toast = Toast.Make("Список избранных маршрутов пуст", ToastDuration.Long);
                    await toast.Show();
                });
                devices = _devices.Where(x => ShowTrackId == -1 || x.TrackId == ShowTrackId).ToList();
                ShowFavoured = false;
            }
        }
        else
        {
            devices = _devices.Where(x => ShowTrackId == -1 || x.TrackId == ShowTrackId).ToList();
        }
        if (devices.Count == 0)
        {
            if (!_showedNoBuses)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var toast = Toast.Make("Автобусов нет на линии", ToastDuration.Long);
                    await toast.Show();
                });
                _showedNoBuses = true;
            }
        }
        foreach (var device in devices)
        {
            if (_incomingIds.TryGetValue(device.Id, out var value) && value == device.IncomingId)
                continue;

            _incomingIds[device.Id] = device.IncomingId;
            
            var busFeature = new PointFeature(SphericalMercator
                .FromLonLat(device.Longitude, device.Latitude).ToMPoint());
            _previousRotations.TryAdd(device.Id, 0);
            
            var rotation = _showTrackIdSet
                ? _previousRotations[device.Id]
                : _previousPoints[device.Id].IsEmpty() 
                    ? busFeature.Point.AngleTo(device.GetLastStopLocation(TrackPolylines)) 
                    : _previousPoints[device.Id].AngleTo(busFeature.Point);
            
            busFeature["number"] = device.TrackName is "6" or "9" ? device.TrackName + "." : device.TrackName;
            busFeature["tag"] = "bus";
            busFeature["rotation"] = rotation;
            busFeature["track_type"] = device.TrackType;
            busFeature["track_id"] = device.TrackId;
            busFeature["bus_number"] = device.Number;
            busFeature["speed"] = (int?)(device.Speed * 1.852);
            busFeature["bus_count"] = devices.Count(x => x.TrackId == device.TrackId);
            busFeature["ID"] = device.Id;

            _previousPoints[device.Id] = busFeature.Point;
            _previousRotations[device.Id] = rotation;
            
            points.Add(busFeature);
        }
        
        _showTrackIdSet = false;
        return points.AsEnumerable();
    }

    void IDynamic.DataHasChanged()
    {
        OnDataChanged();
    }

    private void OnDataChanged()
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? DataChanged;

    public void Dispose()
    {
        _tokenSource.Cancel();
        _tokenSource.Dispose();
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}