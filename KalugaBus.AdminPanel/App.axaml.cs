using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using KalugaBus.AdminPanel.Models;
using KalugaBus.AdminPanel.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KalugaBus.AdminPanel;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);
        
        var collection = new ServiceCollection();
        collection.AddSingleton(new OptionsService<Settings>());
        
        var services = collection.BuildServiceProvider();
        var options = services.GetRequiredService<OptionsService<Settings>>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow(options);
        }

        base.OnFrameworkInitializationCompleted();
    }
}