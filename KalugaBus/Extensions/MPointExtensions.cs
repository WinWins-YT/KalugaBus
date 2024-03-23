using Mapsui;
using Mapsui.Utilities;

namespace KalugaBus.Extensions;

public static class MPointExtensions
{
    public static double AngleTo(this MPoint point1, MPoint? point2)
    {
        if (point2 == null) return 0;
        var result = Algorithms.RadiansToDegrees(Math.Atan2(point1.Y - point2.Y, point2.X - point1.X));
        return result < 0 ? 90.0 + result : result - 270;
    }
}