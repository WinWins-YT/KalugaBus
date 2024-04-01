using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using KalugaBus.Models;

namespace KalugaBus;

public partial class StopInfoPage : ContentPage
{
    private readonly long[] _trackIds;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly HttpClient _httpClient = new();
    private readonly CancellationTokenSource _tokenSource = new();
    private readonly List<Forecast> _forecasts = [];
    public Stop Stop { get; set; }
    public ObservableCollection<StopInfo> StopInfos { get; set; } = [];

    public StopInfoPage(Stop stop, long[] trackIds)
    {
        _trackIds = trackIds;
        Stop = stop;
        
        InitializeComponent();

        BindingContext = this;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    private async void StopInfoPage_OnLoaded(object? sender, EventArgs e)
    {
        IsBusy = true;
        StopInfos.Clear();
        _forecasts.Clear();

        try
        {
            foreach (var trackId in _trackIds)
            {
                _forecasts.AddRange(await FetchInfo(trackId));
            }

            var calculatedTimes = await CalculateEstimatedTimes(_forecasts);
            foreach (var calculatedTime in calculatedTimes)
            {
                StopInfos.Add(calculatedTime);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Произошла ошибка", ex.Message, "OK");
        }

        IsBusy = false;

        await Task.Run(UpdateTime);
    }

    private async Task UpdateTime()
    {
        var cancellationToken = _tokenSource.Token;
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException) {}

            try
            {
                _forecasts.Clear();
                
                foreach (var trackId in _trackIds)
                {
                    _forecasts.AddRange(await FetchInfo(trackId));
                }

                var calculatedTimes = await CalculateEstimatedTimes(_forecasts);
                StopInfos.Clear();
                foreach (var calculatedTime in calculatedTimes)
                {
                    StopInfos.Add(calculatedTime);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Произошла ошибка", ex.Message, "OK");
            }
        }
    }

    private async Task<IEnumerable<Forecast>> FetchInfo(long trackId)
    {
        var forecastJson = await _httpClient.GetStringAsync(
            $"https://bus40.su/default.aspx?target=main&action=get_forecast&station_id={Stop.Station.Id}&track_id={trackId}");
        if (string.IsNullOrEmpty(forecastJson))
            return Array.Empty<Forecast>();
        
        var forecasts =
            JsonSerializer.Deserialize<List<Forecast>>(forecastJson, _jsonSerializerOptions) ??
            throw new InvalidOperationException("Wrong JSON was received from get_forecast");

        return forecasts;
    }

    private Task<IEnumerable<StopInfo>> CalculateEstimatedTimes(IEnumerable<Forecast> forecasts)
    {
        var outputList = new List<StopInfo>();

        foreach (var forecast in forecasts)
        {
            if (forecast.StationLength is null ||
                forecast.AverageSpeed is null ||
                forecast.DevLength is null)
                continue;
            
            if (forecast.StationLength - forecast.DevLength < 0) 
                continue;
            
            var length = forecast.StationLength.Value - forecast.DevLength.Value;
            var avgTime = Math.Round(length / 20 * 60);
            
            if (forecast.AverageSpeed <= 0)
                continue;

            var time = Math.Round(length / forecast.AverageSpeed.Value * 60);
            if (time > 60)
                continue;

            var stopInfo = new StopInfo();
            if (Math.Abs(avgTime - time) < 0.1)
            {
                stopInfo.EstimatedTime = time < 1 ? "< 1 мин." : $"~{avgTime} мин.";
                stopInfo.EstimatedTimeMinValue = time;
                stopInfo.EstimatedTimeMaxValue = time;
            }
            else
            {
                if (avgTime == 0)
                    avgTime = 1;
                if (time == 0)
                    time = 1;
                
                stopInfo.EstimatedTime = $"{Math.Min(time, avgTime)}-{Math.Max(time, avgTime)} мин";
                stopInfo.EstimatedTimeMaxValue = Math.Max(time, avgTime);
                stopInfo.EstimatedTimeMinValue = Math.Min(time, avgTime);
            }

            stopInfo.TrackName = forecast.TrackName;
            stopInfo.Direction = forecast.Direction;
            outputList.Add(stopInfo);
        }

        outputList = outputList.OrderBy(x => x.EstimatedTimeMinValue)
            .ThenBy(x => x.EstimatedTimeMaxValue).ToList();

        return Task.FromResult(outputList.AsEnumerable());
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _tokenSource.Cancel();
    }
}