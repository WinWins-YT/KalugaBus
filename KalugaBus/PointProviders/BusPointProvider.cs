using KalugaBus.Extensions;
using KalugaBus.Styles;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;

namespace KalugaBus.PointProviders;

public class BusPointProvider : MemoryProvider, IDynamic, IDisposable
{
    private readonly CancellationTokenSource _tokenSource = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    public BusPointProvider()
    {
        Catch.TaskRun(RunTimerAsync);
    }

    private readonly Dictionary<string, (double Lon, double Lat)> _previousCoordinates = new()
    {
        ["1"] = (36.240257, 54.514117),
        ["2"] = (36.240557, 54.518117)
    };
    private readonly Dictionary<string, MPoint> _previousPoints = new()
    {
        ["1"] = new MPoint(),
        ["2"] = new MPoint()
    };
    
    private async Task RunTimerAsync()
    {
        while (!_tokenSource.IsCancellationRequested)
        {
            await _timer.WaitForNextTickAsync(_tokenSource.Token);

            foreach (var previousCoordinate in _previousCoordinates.Keys)
            {
                var coords = _previousCoordinates[previousCoordinate];
                _previousCoordinates[previousCoordinate] = (coords.Lon + 0.00005, coords.Lat + 0.00005);
            }

            OnDataChanged();
        }
    }

    public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
    {
        var points = _previousPoints.Keys.Select(x =>
        {
            var busFeature = new PointFeature(SphericalMercator
                .FromLonLat(_previousCoordinates[x].Lon, _previousCoordinates[x].Lat).ToMPoint());
            
            busFeature["number"] = "18";
            busFeature["tag"] = "bus";
            busFeature["rotation"] = _previousPoints[x].AngleTo(busFeature.Point);
            busFeature["track_type"] = 0;
            busFeature["ID"] = x;
            
            _previousPoints[x] = busFeature.Point;
            
            return (IFeature)busFeature;
        });
        
        return Task.FromResult(points);
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