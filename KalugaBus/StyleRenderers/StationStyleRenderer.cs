using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using Mapsui.Utilities;
using SkiaSharp;

namespace KalugaBus.StyleRenderers;

public class StationStyleRenderer : ISkiaStyleRenderer
{
    private readonly SKPicture _busStopPicture = GetPicture("bus_stop.svg");

    public bool Draw(SKCanvas canvas, Viewport viewport, ILayer layer, IFeature feature, IStyle style, IRenderCache renderCache,
        long iteration)
    {
        if (feature is not PointFeature pointFeature || pointFeature["tag"]?.ToString() != "station")
            return false;
        
        var worldPoint = pointFeature.Point;
        var screenPoint = viewport.WorldToScreen(worldPoint);
        
        var fillBrush = new SKPaint { Color = new SKColor(48, 148, 219), IsAntialias = true };
        var radius = (float)Math.Clamp(2 / viewport.Resolution, 0.8, float.MaxValue) * 1.2f;
        
        canvas.Save();
        canvas.Translate((float)screenPoint.X, (float)screenPoint.Y);
        canvas.Scale(radius);
        canvas.DrawPicture(_busStopPicture, fillBrush);
        canvas.Restore();

        return true;
    }
    
    private static SKPicture GetPicture(string embeddedResourcePath)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync(embeddedResourcePath).GetAwaiter().GetResult();
        return stream.LoadSvgPicture();
    }
}