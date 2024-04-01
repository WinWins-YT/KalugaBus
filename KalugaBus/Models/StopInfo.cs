namespace KalugaBus.Models;

public class StopInfo
{
    public string TrackName { get; set; } = "";
    public string Direction { get; set; } = "";
    public double EstimatedTimeMinValue { get; set; }
    public double EstimatedTimeMaxValue { get; set; }
    public string EstimatedTime { get; set; } = "";
}