using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Core.Extensions;
using KalugaBus.Models;

namespace KalugaBus;

public partial class RoutesPage : ContentPage
{
    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    
    public ObservableCollection<RouteDevice> Devices { get; set; } = [];
    
    public RoutesPage()
    {
        InitializeComponent();
        
        BindingContext = this;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    private async void RoutesPage_OnLoaded(object? sender, EventArgs e)
    {
        BusList.IsVisible = false;
        IsBusy = true;

        var devices = await LoadDevices();
        Devices = devices.ToObservableCollection();
        OnPropertyChanged("Devices");
        
        IsBusy = false;
        BusList.IsVisible = true;
    }

    private async Task<IEnumerable<RouteDevice>> LoadDevices()
    {
        var tracksJson = await _httpClient.GetStringAsync("https://danimatcorp.com/bus40/tracks.json");
        var outputList = JsonSerializer.Deserialize<List<RouteDevice>>(tracksJson, _jsonSerializerOptions) ??
                         throw new InvalidOperationException("Wrong JSON was received from get_tracks.json");

        return outputList;
    }

    [GeneratedRegex("<a href=\"#\" onclick=\"SetCurrentRoute\\(this, (\\d+)\\);\">(.{1,20})<\\/a>")]
    private static partial Regex BusIndexRegex();
}