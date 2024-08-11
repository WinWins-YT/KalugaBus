using System.Text.Encodings.Web;
using System.Text.Json;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using KalugaBus.Models;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;

namespace KalugaBus.PointProviders;

public class StationPointProvider : MemoryProvider, IDynamic
{
    private List<TrackStations> _stations = [];
    private readonly Dictionary<long, Station> _stationDict = new();
    
    private readonly HttpClient _httpClient = new();
    
    private long _showTrackId = -1;
    private bool _showStations = true;
    
    public long ShowTrackId
    {
        get => _showTrackId;
        set
        {
            _showTrackId = value;
            OnDataChanged();
        }
    }
    
    public bool ShowStations
    {
        get => _showStations;
        set
        {
            _showStations = value;
            OnDataChanged();
        }
    }

    public StationPointProvider()
    {
        Task.Run(GetStations);
    }
    
    private async Task GetStations()
    {
        try
        {
            var stationsJson = await _httpClient.GetStringAsync("https://bus40.su/default.aspx?target=main&action=get_stations");
            _stations = JsonSerializer.Deserialize<List<TrackStations>>(stationsJson, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }) ?? [];
        } 
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var toast = Toast.Make("Не удалось получить список станций", ToastDuration.Long);
                await toast.Show();
            });
        }
    }

    public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
    {
        if (!_showStations)
            return Task.FromResult(Array.Empty<IFeature>().AsEnumerable());
        
        var features = new List<IFeature>();
        _stationDict.Clear();

        var selectedStations = _showTrackId == -1 ? _stations : _stations.Where(x => x.Id == _showTrackId);
        foreach (var trackStation in selectedStations)
        {
            foreach (var station in trackStation.Stations.DistinctBy(x => x.Id))
            {
                if (_stationDict.All(x => x.Value.Name != station.Name))
                    _stationDict.Add(station.Id, station);

                _stationDict.First(x => x.Value.Name == station.Name).Value.TrackIds.Add(trackStation.Id);
            }
        }

        foreach (var station in _stationDict)
        {
            var feature = new PointFeature(SphericalMercator
                    .FromLonLat(station.Value.Longitude, station.Value.Latitude).ToMPoint());

            feature["tag"] = "station";
            feature["ID"] = station.Value.Id;
            feature["station"] = station.Value;

            features.Add(feature);
        }

        return Task.FromResult(features.AsEnumerable());
    }

    public void DataHasChanged()
    {
        OnDataChanged();
    }
    
    private void OnDataChanged()
    {
        DataChanged?.Invoke(this, new DataChangedEventArgs(null, false, null));
    }

    public event DataChangedEventHandler? DataChanged;
}