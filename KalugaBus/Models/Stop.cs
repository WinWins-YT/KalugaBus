namespace KalugaBus.Models;

public class Stop
{
    public Station Station { get; set; } = new();
    public double Distance { get; set; }
    public string DistanceString { get; set; } = "";
}