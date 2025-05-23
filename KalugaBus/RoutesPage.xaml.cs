﻿using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using KalugaBus.Models;

namespace KalugaBus;

public partial class RoutesPage : ContentPage
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = new TimeSpan(0, 0, 2)
    };
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly List<long> _favouredTracks = [];
    private bool _favouritesChanged;
    
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
        
        var json = Preferences.Get("favoured_tracks", "");
        if (!string.IsNullOrEmpty(json))
            _favouredTracks = JsonSerializer.Deserialize<List<long>>(json)!;
    }

    private async void RoutesPage_OnLoaded(object? sender, EventArgs e)
    {
        if (Devices.Any() && !_favouritesChanged)
            return;
        
        IsBusy = true;
        Devices.Clear();

        try
        {
            var devices = (await LoadDevices()).ToList();
            var favouredDevices = devices.Where(x => _favouredTracks.Contains(x.TrackId)).ToList();
            foreach (var favouredDevice in favouredDevices)
            {
                favouredDevice.IsFavoured = true;
                Devices.Add(favouredDevice);
            }

            foreach (var routeDevice in devices.Except(favouredDevices))
            {
                Devices.Add(routeDevice);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Произошла ошибка", ex.Message, "OK");
        }

        IsBusy = false;
        _favouritesChanged = false;
    }

    private async Task<IEnumerable<RouteDevice>> LoadDevices()
    {
        try
        {
            var tracksJson = await _httpClient.GetStringAsync("https://danimatcorp.com/bus40/tracks.json");
            var outputList = JsonSerializer.Deserialize<List<RouteDevice>>(tracksJson, _jsonSerializerOptions) ??
                             throw new InvalidOperationException("Wrong JSON was received from get_tracks.json");

            var cachedTracksFilePath = Path.Combine(FileSystem.Current.CacheDirectory, "cached_tracks.json");
            await File.WriteAllTextAsync(cachedTracksFilePath, tracksJson);
            
            return outputList;
        }
        catch (Exception)
        {
            var cachedTracksFilePath = Path.Combine(FileSystem.Current.CacheDirectory, "cached_tracks.json");
            if (!File.Exists(cachedTracksFilePath))
                throw;

            var cachedTracks = await File.ReadAllTextAsync(cachedTracksFilePath);
            
            return JsonSerializer.Deserialize<List<RouteDevice>>(cachedTracks, _jsonSerializerOptions) ??
                   throw new InvalidOperationException("Wrong JSON was saved in cached_tracks");
        }
    }

    private async void BusList_OnItemTapped(object? sender, ItemTappedEventArgs e)
    {
        if (e.Item is not RouteDevice device)
            return;
        
        await Shell.Current.GoToAsync($"///{nameof(MainPage)}?TrackId={device.TrackId}");
    }

    private async void MenuItem_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"///{nameof(MainPage)}?ShowFavoured=1");
    }

    private void Favourite_OnClicked(object? sender, EventArgs e)
    {
        var trackId = (sender as ImageButton)?.CommandParameter;
        if (trackId is not long id) 
            return;
        
        var device = Devices.First(x => x.TrackId == id);
        var deviceIndex = Devices.IndexOf(device);
        Devices.Remove(device);
        if (device.IsFavoured)
        {
            device.IsFavoured = false;
            _favouredTracks.Remove(device.TrackId);
        }
        else
        {
            device.IsFavoured = true;
            _favouredTracks.Add(id);
        }
        Devices.Insert(deviceIndex, device);
        _favouritesChanged = true;

        var json = JsonSerializer.Serialize(_favouredTracks);
        Preferences.Set("favoured_tracks", json);
    }
}