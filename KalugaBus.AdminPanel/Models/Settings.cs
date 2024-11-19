using System;

namespace KalugaBus.AdminPanel.Models;

public class Settings
{
    public string WorkingDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
}