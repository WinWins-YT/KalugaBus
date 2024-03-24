using System.Text.Json.Serialization;

namespace KalugaBus.Models;

public class Device
{
    public long Id { get; set; }
    
    [JsonPropertyName("inc_id")]
    public long IncomingId { get; set; }
    
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("lon")]
    public double Longitude { get; set; }
    public long TrackId { get; set; }
    
    [JsonPropertyName("dir")]
    public int Direction { get; set; }
    public float? Speed { get; set; }
    public string TrackName { get; set; } = "";
    public int TrackType { get; set; }
    public string Number { get; set; } = "";
}