
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Input;
using PorkbunDnsUpdater.Core.Models;
using PorkbunDnsUpdater.Core.Services;
using PorkbunDnsUpdater.Core.ViewModels;

namespace PorkbunDnsUpdater.Avalonia;

public sealed class DnsWindow : Window
{
    private readonly AppConfig _config;
    private readonly PorkbunUpdaterService _updater;

    private readonly TextBox _host = new()
    {
        Watermark = "e.g. home",
        Width = 220,
        Height = 34,
        Margin = new Thickness(0, 3, 0, 3),
        HorizontalAlignment = HorizontalAlignment.Left
    };

    private readonly TextBox _domain = new()
    {
        Watermark = "example.com",
        Width = 220,
        Height = 34,
        Margin = new Thickness(0, 3, 0, 3),
        HorizontalAlignment = HorizontalAlignment.Left
    };

    // Each slider position represents one interval (in minutes).
    // Small intervals have fine resolution; longer intervals have larger jumps.
    private static readonly int[] IntervalMinutes =
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
        15, 20, 30, 45, 60, 90, 120, 180, 240,
        360, 480, 720, 1440
    ];

    private readonly Slider _interval = new()
    {
        Minimum = 0,
        Maximum = IntervalMinutes.Length - 1,
        Value = 9, // 10 minutes
        TickFrequency = 1,
        IsSnapToTickEnabled = true,
        Width = 220,
        HorizontalAlignment = HorizontalAlignment.Left
    };

    private readonly TextBlock _intervalLabel = new()
    {
        Text = "10 min",
        VerticalAlignment = VerticalAlignment.Center,
        MinWidth = 85
    };

    private readonly CheckBox _ipv6 = new()
    {
        Content = "Also update IPv6 (AAAA)"
    };

    private readonly TextBlock _ipv4Label = new()
    {
        Text = "Not checked"
    };

    private readonly TextBlock _ipv6Label = new()
    {
        Text = "Not checked"
    };

    private readonly TextBox _log = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap
    };

    private readonly Button _start = new()
    {
        Content = "Start updater",
        IsVisible = true,
        Cursor = new Cursor(StandardCursorType.Hand)
    };

    private readonly Button _stop = new()
    {
        Content = "Stop updater",
        IsVisible = false,
        Cursor = new Cursor(StandardCursorType.Hand)
    };

    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public DnsWindow(
        AppConfig config,
        PorkbunUpdaterService updater)
    {
        _config = config;
        _updater = updater;

        Title = "Porkbun DynDns Updater";
        Width = 660;
        Height = 650;
        MinWidth = 460;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        FontSize = 16;
        InitializeLayout();
        InitializeEvents();

        AppendLog("Ready.");
    }

    private void InitializeLayout()
    {
        var root = new Grid
        {
            Background = CreateBackground()
        };

        var panel = new DockPanel
        {
            Margin = new Thickness(22),
            LastChildFill = true,
            Background = Brushes.Transparent
        };

        var header = CreateHeader();

        DockPanel.SetDock(header, Dock.Top);
        panel.Children.Add(header);

        // 22% black opacity
        _log.Background = new SolidColorBrush(
            Color.FromArgb(55, 0, 0, 0));

        panel.Children.Add(_log);

        root.Children.Add(panel);

        Content = root;
    }

    private static ImageBrush CreateBackground()
    {
        using var stream = AssetLoader.Open(
            new Uri(
                "avares://PorkbunDnsUpdater.Avalonia/Assets/background.jpg"));

        var bitmap = new Bitmap(stream);

        return new ImageBrush
        {
            Source = bitmap,
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center
        };
    }

    private static Image CreatePigHead()
    {
        using var stream = AssetLoader.Open(
            new Uri(
                "avares://PorkbunDnsUpdater.Avalonia/Assets/Justhead.png"));

        return new Image
        {
            Source = new Bitmap(stream),
            Width = 125,
            Height = 125,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
    }

   
    private StackPanel CreateHeader()
    {
        var header = new StackPanel
        {
            Spacing = 10,
            Background = Brushes.Transparent
        };

        var titleRow = new Grid
        {
            Height = 75,
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Background = Brushes.Transparent
        };

        var title = new TextBlock
        {
            Text = "Porkbun DynDns Updater",
            FontSize = 27,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(
                Color.Parse("#A9D8F5")),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 14, 0, 0)
        };

        titleRow.Children.Add(title);

        var pigHead = CreatePigHead();

        // Allow the pig head to extend below the title row.
        titleRow.ClipToBounds = false;

        pigHead.VerticalAlignment = VerticalAlignment.Top;

        Grid.SetColumn(pigHead, 1);
        titleRow.Children.Add(pigHead);

        header.Children.Add(titleRow);
        header.Children.Add(CreateForm());
        header.Children.Add(CreateButtons());

        header.Children.Add(new TextBlock
        {
            Text = "Activity log",
            FontWeight = FontWeight.SemiBold
        });

        header.Children.Add(new TextBlock
        {
            Text = "Closing hides the window in the tray. " +
                   "Choose Quit from the tray menu to exit.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 6)
        });

        return header;
    }


    private Grid CreateForm()
    {
        var form = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("175,*"),
            ColumnSpacing = 12,
            RowSpacing = 10,
            Background = Brushes.Transparent
        };

        for (var i = 0; i < 6; i++)
        {
            form.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
        }

        AddField(form, 0, "Host", _host);
        AddField(form, 1, "Domain", _domain);
        AddField(form, 2, "Check interval", CreateIntervalControl());

        Grid.SetRow(_ipv6, 3);
        Grid.SetColumn(_ipv6, 1);
        form.Children.Add(_ipv6);

        AddField(form, 4, "Public IPv4", _ipv4Label);
        AddField(form, 5, "Public IPv6", _ipv6Label);

        return form;
    }

    private Control CreateIntervalControl()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 15,
            Background = Brushes.Transparent
        };

        panel.Children.Add(_interval);
        panel.Children.Add(_intervalLabel);

        return panel;
    }

    private StackPanel CreateButtons()
    {
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 12, 0, 12),
            Background = Brushes.Transparent
        };

        buttons.Children.Add(_start);
        buttons.Children.Add(_stop);

        return buttons;
    }

    private static void AddField(
        Grid grid,
        int row,
        string label,
        Control control)
    {
        var caption = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetRow(caption, row);
        grid.Children.Add(caption);

        Grid.SetRow(control, row);
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
    }

    private void InitializeEvents()
    {
        _start.Click += async (_, _) => await StartAsync();
        _stop.Click += (_, _) => StopUpdater();

        _interval.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
            {
                _intervalLabel.Text = FormatInterval(GetSelectedIntervalMinutes());
            }
        };
    }

    private int GetSelectedIntervalMinutes()
    {
        var index = Math.Clamp((int)Math.Round(_interval.Value),
            0, IntervalMinutes.Length - 1);
        return IntervalMinutes[index];
    }

    private static string FormatInterval(int minutes)
    {
        if (minutes < 60)
            return $"{minutes} min";

        if (minutes % 60 == 0)
            return $"{minutes / 60} h";

        return $"{minutes / 60} h {minutes % 60} min";
    }

    public void StopUpdater()
    {
        _cts?.Cancel();
    }

    private async Task StartAsync()
    {
        if (_runningTask is { IsCompleted: false })
            return;

        var host = _host.Text?.Trim() ?? "";
        var domain = _domain.Text?.Trim() ?? "";
        var interval = GetSelectedIntervalMinutes();
        var includeIpv6 = _ipv6.IsChecked == true;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(domain))
        {
            AppendLog("Enter both hostname and domain.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.PorkbunApiKey) ||
            string.IsNullOrWhiteSpace(_config.PorkbunApiSecret))
        {
            AppendLog(
                "Missing Porkbun credentials in appsettings.json.");
            return;
        }

        var record = new Record
        {
            HostName = host,
            Domain = domain
        };

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        SetRunning(true);

        var status = new UiProgress<StatusReport>(
            r => AppendLog(r.Content));

        var progress = new UiProgress<ProgressReport>(r =>
        {
            if (r.DnsType == DnsType.A)
                _ipv4Label.Text = r.IP;
            else
                _ipv6Label.Text = r.IP;
        });

        async Task RunUpdater()
        {
            AppendLog("Checking current DNS records...");

            var ipv4 = await _updater.InitDnsUpdaterdater(
                record, DnsType.A, status, token);

            if (string.IsNullOrWhiteSpace(ipv4))
            {
                AppendLog("Could not initialize IPv4 update.");
                return;
            }

            _ipv4Label.Text = ipv4;

            string? ipv6 = null;

            if (includeIpv6)
            {
                ipv6 = await _updater.InitDnsUpdaterdater(
                    record, DnsType.AAAA, status, token);

                if (string.IsNullOrWhiteSpace(ipv6))
                {
                    AppendLog("Could not initialize IPv6 update.");
                    return;
                }

                _ipv6Label.Text = ipv6;
            }
            else
            {
                _ipv6Label.Text = "Disabled";
            }

            AppendLog(
                $"Monitoring every {interval} minute(s). " +
                "You may hide the window in the tray.");

            var tasks = new List<Task>
            {
                _updater.ContinuouslyUpdateIP(
                    record,
                    DnsType.A,
                    ipv4,
                    interval,
                    status,
                    progress,
                    token)
            };

            if (includeIpv6 && ipv6 is not null)
            {
                tasks.Add(
                    _updater.ContinuouslyUpdateIP(
                        record,
                        DnsType.AAAA,
                        ipv6,
                        interval,
                        status,
                        progress,
                        token));
            }

            await Task.WhenAll(tasks);
        }

        _runningTask = RunUpdater();

        try
        {
            await _runningTask;
        }
        catch (OperationCanceledException)
        {
            AppendLog("Updater stopped.");
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            SetRunning(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void SetRunning(bool running)
    {
        _start.IsVisible = !running;
        _stop.IsVisible = running;

        _host.IsEnabled = !running;
        _domain.IsEnabled = !running;
        _interval.IsEnabled = !running;
        _ipv6.IsEnabled = !running;
    }

    private void AppendLog(string message)
    {
        _log.Text +=
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: " +
            $"{message}{Environment.NewLine}";

        _log.CaretIndex = _log.Text?.Length ?? 0;
    }

    private sealed class UiProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public UiProgress(Action<T> report)
        {
            _report = report;
        }

        public void Report(T value)
        {
            Dispatcher.UIThread.Post(() => _report(value));
        }
    }
}
