using KalugaBus.PointProviders;
using KalugaBus.RefactoredMapsUi.Layers.AnimatedLayer;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace KalugaBus.StyleRenders;

public class BusStyleRender : ISkiaStyleRenderer
{
    private readonly SKPaint _redPaint = new() { Color = new SKColor(255, 0, 0), IsAntialias = true };
    private readonly SKPaint _bluePaint = new() { Color = new SKColor(33, 70, 219), IsAntialias = true };
    private readonly SKPaint _textPaint = new() { Color = SKColors.White, TextAlign = SKTextAlign.Center };
    private long _previousIteration;
    
    public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, IRenderCache renderCache,
        long iteration)
    {
        if (feature is not PointFeature pointFeature || pointFeature["tag"]?.ToString() != "bus")
            return false;

        if (layer is not AnimatedPointLayer { DataSource: BusPointProvider pointProvider })
            return false;
        
        var worldPoint = pointFeature.Point;
        var screenPoint = viewport.WorldToScreen(worldPoint);

        var trackType = (int)(feature["track_type"] ?? 1);
        var fillBrush = trackType == 0 ? _bluePaint : _redPaint;

        var radius = (float)Math.Clamp(2 / viewport.Resolution, 0.8, float.MaxValue) * 10;
        _textPaint.TextSize = radius;
        fillBrush.StrokeWidth = radius / 5;
        
        if (pointProvider.ShowTrackId != -1 && _previousIteration != iteration)
        {
            var directBrush = new SKPaint
            {
                Color = new SKColor(51, 45, 237, 200),
                StrokeWidth = radius / 2
            };
            var backBrush = new SKPaint
            {
                Color = new SKColor(237, 55, 45, 200),
                StrokeWidth = radius / 2
            };

            var trackPolyline = pointProvider.TrackPolylines.FirstOrDefault(x => x.Id == pointProvider.ShowTrackId);
            if (trackPolyline is null)
                return false;
            
            for (var i = 0; i < trackPolyline.Data.Direct.Count - 1; i++)
            {
                var point1 = SphericalMercator.FromLonLat
                    (trackPolyline.Data.Direct[i][1], trackPolyline.Data.Direct[i][0]).ToMPoint();
                var point2 = SphericalMercator.FromLonLat
                    (trackPolyline.Data.Direct[i + 1][1], trackPolyline.Data.Direct[i + 1][0]).ToMPoint();

                var screenPoint1 = viewport.WorldToScreen(point1);
                var screenPoint2 = viewport.WorldToScreen(point2);

                canvas.DrawLine((float)screenPoint1.X, (float)screenPoint1.Y, 
                    (float)screenPoint2.X, (float)screenPoint2.Y, directBrush);
            }
            
            for (var i = 0; i < trackPolyline.Data.Back.Count - 1; i++)
            {
                var point1 = SphericalMercator.FromLonLat
                    (trackPolyline.Data.Back[i][1], trackPolyline.Data.Back[i][0]).ToMPoint();
                var point2 = SphericalMercator.FromLonLat
                    (trackPolyline.Data.Back[i + 1][1], trackPolyline.Data.Back[i + 1][0]).ToMPoint();

                var screenPoint1 = viewport.WorldToScreen(point1);
                var screenPoint2 = viewport.WorldToScreen(point2);

                canvas.DrawLine((float)screenPoint1.X, (float)screenPoint1.Y, 
                    (float)screenPoint2.X, (float)screenPoint2.Y, backBrush);
            }

            _previousIteration = iteration;
        }
        
        canvas.Save();
        canvas.Translate((float)screenPoint.X, (float)screenPoint.Y);
        canvas.RotateDegrees((float)(double)pointFeature["rotation"]!);
        canvas.DrawLine(-radius / 2, 0, 0, -radius * 1.5f, fillBrush);
        canvas.DrawLine(radius / 2, 0, 0, -radius * 1.5f, fillBrush);
        canvas.DrawCircle(0, 0, radius, fillBrush);
        canvas.DrawText(feature["number"]?.ToString(), 0, radius / 2, _textPaint);
        canvas.Restore();
        
        return true;
    }
}