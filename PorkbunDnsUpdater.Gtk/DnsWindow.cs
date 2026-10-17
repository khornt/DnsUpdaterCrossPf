using PorkbunDnsUpdater.Models;
using PorkbunDnsUpdater.Services;
using PorkbunDnsUpdater.ViewModels;

internal sealed class DnsWindow
{
    private readonly Gtk.ApplicationWindow _window;
    private readonly AppConfig _config;
    private readonly PorkbunUpdaterService _updater;
    private readonly SynchronizationContext? _uiContext;
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    private Gtk.Entry _host = null!;
    private Gtk.Entry _domain = null!;
    private Gtk.SpinButton _interval = null!;
    private Gtk.CheckButton _ipv6 = null!;
    private Gtk.Label _ipv4Label = null!;
    private Gtk.Label _ipv6Label = null!;
    private Gtk.TextBuffer _logBuffer = null!;
    private Gtk.Button _start = null!;
    private Gtk.Button _stop = null!;

    public DnsWindow(Gtk.ApplicationWindow window, AppConfig config, PorkbunUpdaterService updater)
    {
        _window = window;
        _config = config;
        _updater = updater;
        _uiContext = SynchronizationContext.Current;
    }

    public void Build()
    {
        var outer = Gtk.Box.New(Gtk.Orientation.Vertical, 12);
        outer.SetMarginTop(18);
        outer.SetMarginBottom(18);
        outer.SetMarginStart(22);
        outer.SetMarginEnd(22);
        outer.Append(Gtk.Label.New("Porkbun Dynamic DNS Updater"));

        var form = Gtk.Grid.New();
        form.SetRowSpacing(10);
        form.SetColumnSpacing(12);
        outer.Append(form);

        _host = Gtk.Entry.New();
        _host.PlaceholderText = "e.g. home";
        _domain = Gtk.Entry.New();
        _domain.PlaceholderText = "example.com";
        _interval = Gtk.SpinButton.NewWithRange(1, 1440, 1);
        _interval.SetValue(10);
        _ipv6 = Gtk.CheckButton.NewWithLabel("Also update IPv6 (AAAA)");

        AddRow(form, 0, "Host", _host);
        AddRow(form, 1, "Domain", _domain);
        AddRow(form, 2, "Check interval (minutes)", _interval);
        form.Attach(_ipv6, 1, 3, 1, 1);

        _ipv4Label = Gtk.Label.New("Not checked");
        _ipv6Label = Gtk.Label.New("Not checked");
        AddRow(form, 4, "Public IPv4", _ipv4Label);
        AddRow(form, 5, "Public IPv6", _ipv6Label);

        var actions = Gtk.Box.New(Gtk.Orientation.Horizontal, 8);
        _start = Gtk.Button.NewWithLabel("Start updater");
        _stop = Gtk.Button.NewWithLabel("Stop");
        _stop.SetSensitive(false);
        actions.Append(_start);
        actions.Append(_stop);
        outer.Append(actions);

        outer.Append(Gtk.Label.New("Activity log"));
        var logView = Gtk.TextView.New();
        logView.SetEditable(false);
        logView.SetCursorVisible(false);
        logView.SetWrapMode(Gtk.WrapMode.WordChar);
        _logBuffer = logView.GetBuffer();
        var scroller = Gtk.ScrolledWindow.New();
        scroller.SetMinContentHeight(220);
        scroller.SetChild(logView);
        scroller.SetVexpand(true);
        outer.Append(scroller);

        _start.OnClicked += (_, _) => _ = StartAsync();
        _stop.OnClicked += (_, _) => _cts?.Cancel();
        _window.OnCloseRequest += (_, _) => { _cts?.Cancel(); return false; };
        _window.Child = outer;
        AppendLog("Ready. Set PORKBUN_API_KEY and PORKBUN_API_SECRET in the environment before starting.");
    }

    private static void AddRow(Gtk.Grid grid, int row, string label, Gtk.Widget input)
    {
        var caption = Gtk.Label.New(label);
        caption.SetXalign(0);
        grid.Attach(caption, 0, row, 1, 1);
        grid.Attach(input, 1, row, 1, 1);
    }

    private async Task StartAsync()
    {
        if (_runningTask is { IsCompleted: false }) return;
        var host = _host.GetText().Trim();
        var domain = _domain.GetText().Trim();
        var interval = (int)_interval.GetValue();
        var includeIpv6 = _ipv6.GetActive();
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(domain))
        { AppendLog("Enter both hostname and domain."); return; }
        if (string.IsNullOrWhiteSpace(_config.PorkbunApiKey) || string.IsNullOrWhiteSpace(_config.PorkbunApiSecret))
        { AppendLog("Missing Porkbun credentials. Set environment variables first."); return; }

        var record = new Record { HostName = host, Domain = domain };
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _start.SetSensitive(false);
        _stop.SetSensitive(true);
        _host.SetSensitive(false);
        _domain.SetSensitive(false);
        _interval.SetSensitive(false);
        _ipv6.SetSensitive(false);

        var status = new InlineProgress<StatusReport>(r => PostUi(() => AppendLog(r.Content)));
        var progress = new InlineProgress<ProgressReport>(r => PostUi(() =>
        {
            if (r.DnsType == DnsType.A) _ipv4Label.SetText(r.IP);
            else _ipv6Label.SetText(r.IP);
        }));
        _runningTask = UpdateAsync();
        try { await _runningTask; }
        catch (OperationCanceledException) { AppendLog("Updater stopped."); }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
        finally
        {
            _start.SetSensitive(true);
            _stop.SetSensitive(false);
            _host.SetSensitive(true);
            _domain.SetSensitive(true);
            _interval.SetSensitive(true);
            _ipv6.SetSensitive(true);
            _cts?.Dispose();
            _cts = null;
        }

        async Task UpdateAsync()
        {
            AppendLog("Checking current DNS records...");
            var ipv4 = await _updater.InitDnsUpdaterdater(record, DnsType.A, status, ct);
            if (string.IsNullOrEmpty(ipv4)) { AppendLog("Unable to initialize IPv4 updater."); return; }
            _ipv4Label.SetText(ipv4);
            string? ipv6 = null;
            if (includeIpv6)
            {
                ipv6 = await _updater.InitDnsUpdaterdater(record, DnsType.AAAA, status, ct);
                if (string.IsNullOrEmpty(ipv6)) { AppendLog("Unable to initialize IPv6 updater."); return; }
                _ipv6Label.SetText(ipv6);
            }
            else _ipv6Label.SetText("Disabled");
            AppendLog($"Monitoring every {interval} minute(s). Minimize the window to keep running.");
            var tasks = new List<Task>
            {
                _updater.ContinuouslyUpdateIP(record, DnsType.A, ipv4, interval, status, progress, ct)
            };
            if (includeIpv6 && ipv6 is not null)
                tasks.Add(_updater.ContinuouslyUpdateIP(record, DnsType.AAAA, ipv6, interval, status, progress, ct));
            await Task.WhenAll(tasks);
        }
    }

    private void PostUi(Action action)
    {
        if (_uiContext is null) action();
        else _uiContext.Post(_ => action(), null);
    }

    private void AppendLog(string message)
    {
        var end = _logBuffer.GetEndIter();
        _logBuffer.Insert(ref end, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}\n", -1);
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;
        public InlineProgress(Action<T> callback) => _callback = callback;
        public void Report(T value) => _callback(value);
    }
}
