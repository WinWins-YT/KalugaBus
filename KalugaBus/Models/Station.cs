using System.Text.Json.Serialization;

namespace KalugaBus.Models;

public class Station
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    [JsonPropertyName("lat")] public double Latitude { get; set; }
    [JsonPropertyName("lon")] public double Longitude { get; set; }
    [JsonIgnore] public HashSet<long> TrackIds { get; set; } = [];
}