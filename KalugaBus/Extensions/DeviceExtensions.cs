using KalugaBus.Enums;
using KalugaBus.Models;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;

namespace KalugaBus.Extensions;

static class DeviceExtensions
{
    public static MPoint GetLastStopLocation(this Models.Device device, List<TrackPolyline> polylines)
    {
        var polyline = polylines.First(x => x.Id == device.TrackId);
        var angle = device.Direction switch
        {
            Direction.Direct => SphericalMercator
                .FromLonLat(polyline.Data.Direct.Last()[1], polyline.Data.Direct.Last()[0]).ToMPoint(),
            Direction.Back => SphericalMercator
                .FromLonLat(polyline.Data.Back.Last()[1], polyline.Data.Back.Last()[0]).ToMPoint(),
            _ => new MPoint()
        };
        return angle;
    }
}