using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.Configuration;
using PorkbunDnsUpdater.AvaloniaApp;
using PorkbunDnsUpdater.Core.Backend.PorkBun.WebClient;
using PorkbunDnsUpdater.Core.Models;
using PorkbunDnsUpdater.Core.Services;

namespace PorkbunDnsUpdater.Avalonia;

public sealed class App : Application
{
    private DnsWindow? _window;
    private bool _quitting;

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var config = configuration
                             .GetSection("Porkbun")
                             .Get<AppConfig>()
                         ?? throw new InvalidOperationException(
                             "Missing Porkbun configuration.");
            
            var updater = new PorkbunUpdaterService(new PorkbunHttpClient(config));
            _window = new DnsWindow(config, updater);
            
            using var stream = AssetLoader.Open(
                new Uri("avares://PorkbunDnsUpdater.Avalonia/Assets/tray.ico"));

            _window.Icon = new WindowIcon(stream);
            
            _window.Closing += (_, e) =>
            {
                if (_quitting) return;
                e.Cancel = true;
                _window.Hide();
            };
            desktop.MainWindow = _window;
            ConfigureTray(desktop);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var show = new NativeMenuItem("Show window");
        show.Click += (_, _) => ShowWindow();

        var quit = new NativeMenuItem("Quit");
        quit.Click += (_, _) => Quit(desktop);

        var menu = new NativeMenu();
        menu.Add(show);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(quit);

        // Load the icon from Avalonia's embedded resources
        using var stream = AssetLoader.Open(
            new Uri("avares://PorkbunDnsUpdater.Avalonia/Assets/tray.ico"));

        var icon = new TrayIcon
        {
            Icon = new WindowIcon(stream),
            ToolTipText = "Porkbun DNS Updater",
            Menu = menu,
            IsVisible = true
        };

        icon.Clicked += (_, _) => ShowWindow();

        TrayIcon.SetIcons(this, new TrayIcons { icon });
    }

    private void ShowWindow()
    {
        if (_window is null) return;
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void Quit(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _quitting = true;
        _window?.StopUpdater();
        desktop.Shutdown();
    }
}
