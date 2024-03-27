using System.Text.Encodings.Web;
using System.Text.Json;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using KalugaBus.Extensions;
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
    private readonly Dictionary<long, long> _incomingIds = new();

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
    
    private async Task FetchBusDataAsync()
    {
        while (!_tokenSource.IsCancellationRequested)
        {
            await _timer.WaitForNextTickAsync(_tokenSource.Token);

            try
            {
                var json = await _httpClient.GetStringAsync(
                    "https://bus40.su/default.aspx?target=main&action=get_devices");
                _devices = JsonSerializer.Deserialize<List<Device>>(json, _jsonSerializerOptions) ??
                           throw new InvalidOperationException("Wrong JSON was received");
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var toast = Toast.Make($"Произошла ошибка:\n{ex.Message}", ToastDuration.Long);
                    await toast.Show();
                });
            }

            foreach (var device in _devices.Where(device => !_previousPoints.ContainsKey(device.Id)))
            {
                _previousPoints[device.Id] = new MPoint();
            }

            OnDataChanged();
        }
    }

    public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
    {
        List<IFeature> points = [];
        foreach (var device in _devices)
        {
            if (_incomingIds.TryGetValue(device.Id, out var value) && value == device.IncomingId)
                continue;

            _incomingIds[device.Id] = device.IncomingId;
            
            var busFeature = new PointFeature(SphericalMercator
                .FromLonLat(device.Longitude, device.Latitude).ToMPoint());
            
            busFeature["number"] = device.TrackName is "6" or "9" ? device.TrackName + "." : device.TrackName;
            busFeature["tag"] = "bus";
            busFeature["rotation"] = _previousPoints[device.Id].AngleTo(busFeature.Point);
            busFeature["track_type"] = device.TrackType;
            busFeature["track_id"] = device.TrackId;
            busFeature["ID"] = device.Id;

            _previousPoints[device.Id] = busFeature.Point;
            
            points.Add(busFeature);
        }
        
        return Task.FromResult(points.AsEnumerable());
    }

    void IDynamic.DataHasChanged()
    {
        OnDataChanged();
    }

    private void OnDataChanged()
    {
        DataChanged?.Invoke(this, new DataChangedEventArgs(null, false, null));
    }

    public event DataChangedEventHandler? DataChanged;

    public void Dispose()
    {
        _tokenSource.Cancel();
        _tokenSource.Dispose();
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}