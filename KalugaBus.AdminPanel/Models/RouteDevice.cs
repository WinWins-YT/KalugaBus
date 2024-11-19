namespace KalugaBus.AdminPanel.Models;

public class RouteDevice
{
    public long TrackId { get; set; }
    public string ImageUrl { get; set; } = "";
    public string Name { get; set; } = "";
    public string Route { get; set; } = "";
    public bool IsFavoured { get; set; }
}