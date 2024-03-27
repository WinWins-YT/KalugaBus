using System.Text.Json.Serialization;

namespace KalugaBus.Models;

public class TrackStations
{
    public long Id { get; set; }
    [JsonPropertyName("data")] public List<Station> Stations { get; set; } = [];
}