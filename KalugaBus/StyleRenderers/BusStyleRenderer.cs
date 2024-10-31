using KalugaBus.PointProviders;
using KalugaBus.RefactoredMapsUi.Layers.AnimatedLayer;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace KalugaBus.StyleRenderers;

public class BusStyleRenderer : ISkiaStyleRenderer
{
    private readonly SKPaint _redPaint = new() { Color = new SKColor(255, 0, 0), IsAntialias = true };
    private readonly SKPaint _bluePaint = new() { Color = new SKColor(33, 70, 219), IsAntialias = true };
    private readonly SKPaint _textPaint = new() { Color = SKColors.White, TextAlign = SKTextAlign.Center };
    
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