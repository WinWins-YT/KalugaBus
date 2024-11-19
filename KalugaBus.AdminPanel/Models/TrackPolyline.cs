namespace KalugaBus.AdminPanel.Models;

public class TrackPolyline
{
    public long Id { get; set; }
    public Polyline Data { get; set; } = new();
}