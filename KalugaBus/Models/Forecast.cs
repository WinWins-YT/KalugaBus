using System.Text.Json.Serialization;

namespace KalugaBus.Models;

public class Forecast
{
    public string TrackName { get; set; } = "";
    public long TrackId { get; set; }
    [JsonPropertyName("dir")] public string Direction { get; set; } = "";
    //public double? Speed { get; set; }
    [JsonPropertyName("avg_speed")] public double? AverageSpeed { get; set; }
    public double? StationLength { get; set; }
    public double? DevLength { get; set; }
}