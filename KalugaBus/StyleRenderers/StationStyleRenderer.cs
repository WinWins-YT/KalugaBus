using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace KalugaBus.StyleRenderers;

public class StationStyleRenderer : ISkiaStyleRenderer
{
    public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, IRenderCache renderCache,
        long iteration)
    {
        if (feature is not PointFeature pointFeature || pointFeature["tag"]?.ToString() != "station")
            return false;
        
        var worldPoint = pointFeature.Point;
        var screenPoint = viewport.WorldToScreen(worldPoint);
        
        var fillBrush = new SKPaint { Color = new SKColor(50, 168, 82), IsAntialias = true };
        var radius = (float)Math.Clamp(2 / viewport.Resolution, 0.8, float.MaxValue) * 7;
        
        canvas.Save();
        canvas.Translate((float)screenPoint.X, (float)screenPoint.Y);
        canvas.DrawCircle(0, 0, radius, fillBrush);
        canvas.Restore();

        return true;
    }
}