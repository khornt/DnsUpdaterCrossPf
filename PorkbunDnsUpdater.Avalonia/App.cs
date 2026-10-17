using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Platform;
using PorkbunDnsUpdater.Backend.PorkBun.WebClient;
using PorkbunDnsUpdater.Models;
using PorkbunDnsUpdater.Services;

namespace PorkbunDnsUpdater.AvaloniaApp;

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
            var config = new AppConfig();
            var updater = new PorkbunUpdaterService(new PorkbunHttpClient(config));
            _window = new DnsWindow(config, updater);
            // _window.Closing += (_, e) =>
            // {
            //     if (_quitting) return;
            //     e.Cancel = true;
            //     _window.Hide();
            // };
            desktop.MainWindow = _window;
            // ConfigureTray(desktop);
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

        var icon = new TrayIcon
        {
            Icon = new WindowIcon("avares://PorkbunDnsUpdater.Avalonia/Assets/tray.ico"),
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
