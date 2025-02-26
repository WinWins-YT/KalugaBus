using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace KalugaBus.AdminPanel.Models;

public class RouteDevice
{
    public long TrackId { get; set; }
    public string ImageUrl { get; set; } = "";
    public IImage Image => new Bitmap(AssetLoader.Open(new Uri(ImageUrl)));
    public string Name { get; set; } = "";
    public string Route { get; set; } = "";
    public bool IsFavoured { get; set; }
}