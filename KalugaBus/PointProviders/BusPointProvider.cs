using KalugaBus.Extensions;
using KalugaBus.Styles;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;

namespace KalugaBus.PointProviders;

public class BusPointProvider : MemoryProvider, IDynamic
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    public BusPointProvider()
    {
        Catch.TaskRun(RunTimerAsync);
    }

    private (double Lon, double Lat) _previousCoordinates = (36.240257, 54.514117);
    private MPoint _previousPoint = new();
    private async Task RunTimerAsync()
    {
        while (true)
        {
            await _timer.WaitForNextTickAsync();

            _previousCoordinates = (_previousCoordinates.Lon + 0.00005, _previousCoordinates.Lat + 0.00005);

            OnDataChanged();
        }
    }

    public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
    {
        var points = new List<IFeature>();
        var busFeature = new PointFeature(SphericalMercator.FromLonLat(_previousCoordinates.Lon, _previousCoordinates.Lat).ToMPoint());
        busFeature["number"] = "18";
        busFeature["tag"] = "bus";
        busFeature["rotation"] = _previousPoint.AngleOf(busFeature.Point);
        busFeature["ID"] = "1";
        points.Add(busFeature);
        _previousPoint = busFeature.Point;
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
}