using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PorkbunDnsUpdater.Core.Models;
using PorkbunDnsUpdater.Core.Services;
using PorkbunDnsUpdater.Core.ViewModels;

namespace PorkbunDnsUpdater.Avalonia;

public sealed class DnsWindow : Window
{
    private readonly AppConfig _config;
    private readonly PorkbunUpdaterService _updater;
    private readonly TextBox _host = new() { Watermark = "e.g. home" };
    private readonly TextBox _domain = new() { Watermark = "example.com" };
    private readonly NumericUpDown _interval = new() { Minimum = 1, Maximum = 1440, Increment = 1, Value = 10 };
    private readonly CheckBox _ipv6 = new() { Content = "Also update IPv6 (AAAA)" };
    private readonly TextBlock _ipv4Label = new() { Text = "Not checked" };
    private readonly TextBlock _ipv6Label = new() { Text = "Not checked" };
    private readonly TextBox _log = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
    private readonly Button _start = new() { Content = "Start updater" };
    private readonly Button _stop = new() { Content = "Stop", IsEnabled = false };
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public DnsWindow(AppConfig config, PorkbunUpdaterService updater)
    {
        _config = config;
        _updater = updater;
        Title = "Porkbun Dynamic DNS Updater";
        Width = 660;
        Height = 650;
        MinWidth = 460;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var form = new Grid { ColumnDefinitions = new ColumnDefinitions("175,*"), ColumnSpacing = 12, RowSpacing = 10 };
        for (var i = 0; i < 6; i++) form.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddField(form, 0, "Host", _host);
        AddField(form, 1, "Domain", _domain);
        AddField(form, 2, "Check interval (minutes)", _interval);
        Grid.SetRow(_ipv6, 3); Grid.SetColumn(_ipv6, 1); form.Children.Add(_ipv6);
        AddField(form, 4, "Public IPv4", _ipv4Label);
        AddField(form, 5, "Public IPv6", _ipv6Label);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        buttons.Children.Add(_start);
        buttons.Children.Add(_stop);

        var panel = new DockPanel { Margin = new Thickness(22), LastChildFill = true };
        var header = new StackPanel { Spacing = 14 };
        header.Children.Add(new TextBlock { Text = "Porkbun Dynamic DNS Updater", FontSize = 21, FontWeight = FontWeight.SemiBold });
        header.Children.Add(form);
        header.Children.Add(buttons);
        header.Children.Add(new TextBlock { Text = "Activity log", FontWeight = FontWeight.SemiBold });
        header.Children.Add(new TextBlock { Text = "Closing hides the window in the tray. Choose Quit from the tray menu to exit.", TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        DockPanel.SetDock(header, Dock.Top);
        panel.Children.Add(header);
        panel.Children.Add(_log);
        Content = panel;
        _start.Click += async (_, _) => await StartAsync();
        _stop.Click += (_, _) => StopUpdater();
        AppendLog("Ready. Configure PORKBUN_API_KEY and PORKBUN_API_SECRET in the environment.");
    }

    private static void AddField(Grid grid, int row, string label, Control control)
    {
        var caption = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(caption, row);
        grid.Children.Add(caption);
        Grid.SetRow(control, row); Grid.SetColumn(control, 1);
        grid.Children.Add(control);
    }

    public void StopUpdater() => _cts?.Cancel();

    private async Task StartAsync()
    {
        if (_runningTask is { IsCompleted: false }) return;
        var host = _host.Text?.Trim() ?? "";
        var domain = _domain.Text?.Trim() ?? "";
        var interval = (int)(_interval.Value ?? 10);
        var includeIpv6 = _ipv6.IsChecked == true;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(domain))
        { AppendLog("Enter both hostname and domain."); return; }
        if (string.IsNullOrWhiteSpace(_config.PorkbunApiKey) || string.IsNullOrWhiteSpace(_config.PorkbunApiSecret))
        { AppendLog("Missing Porkbun credentials (environment variables)."); return; }

        var record = new Record { HostName = host, Domain = domain };
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        SetRunning(true);
        var status = new UiProgress<StatusReport>(r => AppendLog(r.Content));
        var progress = new UiProgress<ProgressReport>(r =>
        {
            if (r.DnsType == DnsType.A) _ipv4Label.Text = r.IP;
            else _ipv6Label.Text = r.IP;
        });

        async Task RunUpdater()
        {
            AppendLog("Checking current DNS records...");
            var ipv4 = await _updater.InitDnsUpdaterdater(record, DnsType.A, status, token);
            if (string.IsNullOrWhiteSpace(ipv4)) { AppendLog("Could not initialize IPv4 update."); return; }
            _ipv4Label.Text = ipv4;
            string? ipv6 = null;
            if (includeIpv6)
            {
                ipv6 = await _updater.InitDnsUpdaterdater(record, DnsType.AAAA, status, token);
                if (string.IsNullOrWhiteSpace(ipv6)) { AppendLog("Could not initialize IPv6 update."); return; }
                _ipv6Label.Text = ipv6;
            }
            else _ipv6Label.Text = "Disabled";
            AppendLog($"Monitoring every {interval} minute(s). You may hide the window in the tray.");
            var tasks = new List<Task> { _updater.ContinuouslyUpdateIP(record, DnsType.A, ipv4, interval, status, progress, token) };
            if (includeIpv6 && ipv6 is not null)
                tasks.Add(_updater.ContinuouslyUpdateIP(record, DnsType.AAAA, ipv6, interval, status, progress, token));
            await Task.WhenAll(tasks);
        }
        _runningTask = RunUpdater();
        try { await _runningTask; }
        catch (OperationCanceledException) { AppendLog("Updater stopped."); }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
        finally
        {
            SetRunning(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void SetRunning(bool running)
    {
        _start.IsEnabled = !running;
        _stop.IsEnabled = running;
        _host.IsEnabled = !running;
        _domain.IsEnabled = !running;
        _interval.IsEnabled = !running;
        _ipv6.IsEnabled = !running;
    }

    private void AppendLog(string message)
    {
        _log.Text += $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}{Environment.NewLine}";
        _log.CaretIndex = _log.Text?.Length ?? 0;
    }

    private sealed class UiProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;
        public UiProgress(Action<T> report) => _report = report;
        public void Report(T value) => Dispatcher.UIThread.Post(() => _report(value));
    }
}
