using Mapsui.Styles;

namespace KalugaBus.Styles;

public class StationStyle : IStyle
{
    public double MinVisible { get; set; }
    public double MaxVisible { get; set; } = 30;
    public bool Enabled { get; set; } = true;
    public float Opacity { get; set; } = 0.7f;
}