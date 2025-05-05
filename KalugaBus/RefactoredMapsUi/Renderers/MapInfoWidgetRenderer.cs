using Mapsui;
using Mapsui.Rendering.Skia.Cache;
using Mapsui.Rendering.Skia.Extensions;
using Mapsui.Rendering.Skia.SkiaWidgets;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using SkiaSharp;
using Topten.RichTextKit;

namespace KalugaBus.RefactoredMapsUi.Renderers;

public class MapInfoWidgetRenderer : ISkiaWidgetRenderer
{
    public void Draw(SKCanvas canvas, Viewport viewport, IWidget widget, RenderService renderService,
        float layerOpacity)
    {
        DrawText(canvas, viewport, widget, layerOpacity);
    }

    public static void DrawText(SKCanvas canvas, Viewport viewport, IWidget widget, float layerOpacity)
    {
        var mapInfoWidget = (MapInfoWidget)widget;

        if (string.IsNullOrEmpty(mapInfoWidget.Text)) return;

        var rs = new RichString()
            .Add(mapInfoWidget.Text, 
                fontSize: (float)mapInfoWidget.TextSize, 
                textColor: mapInfoWidget.TextColor.ToSkia((float)mapInfoWidget.Opacity));
        rs.MaxWidth = (float)viewport.Width - 20;
        
        var textRect = new SKRect(0, 0, rs.MeasuredWidth, rs.MeasuredHeight);

        using var backPaint = new SKPaint();
        backPaint.Color = mapInfoWidget.BackColor.ToSkia((float)mapInfoWidget.Opacity);
        backPaint.IsAntialias = true;

        var paddingX = mapInfoWidget.Padding.Left;
        var paddingY = mapInfoWidget.Padding.Top;

        if (mapInfoWidget.Width != 0)
        {
            // TextBox has a width, so use this
            paddingX = (mapInfoWidget.Width - textRect.Width) / 2.0f;
            textRect = new SKRect(textRect.Left, textRect.Top, (float)(textRect.Left + mapInfoWidget.Width - paddingX * 2), textRect.Bottom);
        }

        if (mapInfoWidget.Height != 0)
        {
            // TextBox has a height, so use this
            paddingY = (mapInfoWidget.Height - (float)mapInfoWidget.TextSize) / 2.0f;
            textRect = new SKRect(textRect.Left, textRect.Top, textRect.Right, (float)(textRect.Top + mapInfoWidget.Height - paddingY * 2));
        }

        mapInfoWidget.UpdateEnvelope(
            mapInfoWidget.Width != 0 ? mapInfoWidget.Width : textRect.Width + mapInfoWidget.Padding.Left + mapInfoWidget.Padding.Right,
            mapInfoWidget.Height != 0 ? mapInfoWidget.Height : textRect.Height + mapInfoWidget.Padding.Top + mapInfoWidget.Padding.Bottom,
            viewport.Width,
            viewport.Height);

        if (mapInfoWidget.Envelope == null)
            return;

        canvas.DrawRoundRect(mapInfoWidget.Envelope.ToSkia(), (float)mapInfoWidget.CornerRadius, (float)mapInfoWidget.CornerRadius, backPaint);

        rs.Paint(canvas, new SKPoint((float)(mapInfoWidget.Envelope.MinX - textRect.Left + paddingX),
                (float)(mapInfoWidget.Envelope.MinY - textRect.Top + paddingY)),
            new TextPaintOptions { Edging = SKFontEdging.Antialias });
    }
}