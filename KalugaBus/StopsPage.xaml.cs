using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using KalugaBus.Models;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace KalugaBus;

public partial class StopsPage : ContentPage
{
    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly Dictionary<Stop, HashSet<long>> _stationDict = new();
    private Location? _userLocation;

    public ObservableCollection<Stop> Stops { get; set; } = [];
    
    public StopsPage()
    {
        InitializeComponent();

        BindingContext = this;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    private async void StopsPage_OnLoaded(object? sender, EventArgs e)
    {
        if (Stops.Any())
            return;
        
        IsBusy = true;
        Stops.Clear();

        if (await CheckLocationPermission() != PermissionStatus.Granted)
            await DisplayAlert("Местоположение недоступно",
                "Чтобы видеть остановки в порядке отдаления от них, необходимо разрешить местоположение", "OK");
        else
        {
            _userLocation = await Geolocation.GetLocationAsync();
        }
        
        var stops = await FetchData();
        foreach (var stop in stops)
        {
            Stops.Add(stop);
        }

        IsBusy = false;
    }

    private static async Task<PermissionStatus> CheckLocationPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status == PermissionStatus.Granted)
            return status;

        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        return status;
    }

    private async Task<IEnumerable<Stop>> FetchData()
    {
        var trackStationsJson =
            await _httpClient.GetStringAsync("https://bus40.su/default.aspx?target=main&action=get_stations");
        var trackStations =
            JsonSerializer.Deserialize<List<TrackStations>>(trackStationsJson, _jsonSerializerOptions) ??
            throw new InvalidOperationException("Wrong JSON was received from get_stations");
        foreach (var trackStation in trackStations)
        {
            trackStation.Stations = trackStation.Stations.DistinctBy(x => x.Id).ToList();
        }

        foreach (var trackStation in trackStations)
        {
            foreach (var station in trackStation.Stations)
            {
                if (_stationDict.All(x => x.Key.Station.Name != station.Name))
                    _stationDict.Add(new Stop
                    {
                        Station = station
                    }, []);

                _stationDict.First(x => x.Key.Station.Name == station.Name).Value.Add(trackStation.Id);
            }
        }

        return _userLocation is not null ? await CalculateDistances(_stationDict.Keys) : _stationDict.Keys;
    }

    private Task<IEnumerable<Stop>> CalculateDistances(IEnumerable<Stop> stops)
    {
        var stopsList = stops.ToList();
        
        foreach (var stop in stopsList)
        {
            var distance = _userLocation.CalculateDistance(stop.Station.Latitude, stop.Station.Longitude,
                DistanceUnits.Kilometers);
            stop.Distance = distance;
            stop.DistanceString = distance < 1 ? $"{Math.Round(distance * 1000)} м." : $"{Math.Round(distance, 3)} км.";
        }

        return Task.FromResult(stopsList.OrderBy(x => x.Distance).AsEnumerable());
    }
}