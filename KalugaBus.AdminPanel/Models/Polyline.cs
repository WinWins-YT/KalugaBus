using System.Collections.Generic;

namespace KalugaBus.AdminPanel.Models;

public class Polyline
{
    public List<double[]> Direct { get; set; } = [];
    public List<double[]> Back { get; set; } = [];
}