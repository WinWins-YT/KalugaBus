namespace KalugaBus.Extensions;

public static class LocationExtensions
{
    public static void Deconstruct(this Location location, out double latitude, out double longitude)
    {
        latitude = location.Latitude;
        longitude = location.Longitude;
    }
}