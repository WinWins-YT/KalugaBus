using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;
using Debug = System.Diagnostics.Debug;

namespace KalugaBus.StyleRenders;

public class BusStyleRender : ISkiaStyleRenderer
{
    public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, IRenderCache renderCache,
        long iteration)
    {
        if (feature is not PointFeature pointFeature || pointFeature["tag"]?.ToString() != "bus")
            return false;
        
        var worldPoint = pointFeature.Point;
        var screenPoint = viewport.WorldToScreen(worldPoint);

        var fillColor = new SKColor(255, 0, 0);
        var fillBrush = new SKPaint { Color = fillColor, IsAntialias = true };
        var textBrush = new SKPaint { Color = SKColors.White, TextAlign = SKTextAlign.Center };

        canvas.Save();
        canvas.Translate((float)screenPoint.X, (float)screenPoint.Y);
        canvas.RotateDegrees((float)((double)pointFeature["rotation"]! - 270));
        var radius = 2 / (float)viewport.Resolution;
        textBrush.TextSize = radius * 10;
        canvas.DrawCircle(0, 0, 10 * radius, fillBrush);
        canvas.DrawText(feature["number"]?.ToString(), 0, radius * 5, textBrush);
        canvas.Restore();
        
        return true;
    }
}