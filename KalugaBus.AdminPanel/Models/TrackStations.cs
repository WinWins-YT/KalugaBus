using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KalugaBus.AdminPanel.Models;

public class TrackStations
{
    public long Id { get; set; }
    [JsonPropertyName("data")] public List<Station> Stations { get; set; } = [];
}