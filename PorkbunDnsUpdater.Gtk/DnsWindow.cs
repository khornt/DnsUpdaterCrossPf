using PorkbunDnsUpdater.Core.Models;
using PorkbunDnsUpdater.Core.Services;
using PorkbunDnsUpdater.Core.ViewModels;

namespace PorkbunDnsUpdater.Gtk;

internal sealed class DnsWindow
{
    private readonly global::Gtk.ApplicationWindow _window;
    private readonly AppConfig _config;
    private readonly PorkbunUpdaterService _updater;
    private readonly SynchronizationContext? _uiContext;
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    private global::Gtk.Entry _host = null!;
    private global::Gtk.Entry _domain = null!;
    private static readonly int[] IntervalMinutes =
    [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 20, 30, 45, 60, 90, 120, 180, 240, 360, 480, 720, 1440];
    private global::Gtk.Scale _interval = null!;
    private global::Gtk.Label _intervalLabel = null!;
    private global::Gtk.CheckButton _ipv6 = null!;
    private global::Gtk.Label _ipv4Label = null!;
    private global::Gtk.Label _ipv6Label = null!;
    private global::Gtk.TextBuffer _logBuffer = null!;
    private global::Gtk.Button _start = null!;
    private global::Gtk.Button _stop = null!;

    public DnsWindow(global::Gtk.ApplicationWindow window, AppConfig config, PorkbunUpdaterService updater)
    {
        _window = window;
        _config = config;
        _updater = updater;
        _uiContext = SynchronizationContext.Current;
    }

    public void Build()
    {
        ApplyStyles();
        _window.Title = "Porkbun DynDns Updater";
        ConfigureWindowIcon();
        _window.SetDefaultSize(660, 650);

        var root = global::Gtk.Overlay.New();

        var backgroundPath = Path.Combine(
            AppContext.BaseDirectory, "Assets", "background.jpg");

        if (File.Exists(backgroundPath))
        {
            var background = global::Gtk.Picture.NewForFilename(backgroundPath);
            background.SetContentFit(global::Gtk.ContentFit.Cover);
            background.SetCanShrink(true);
            root.SetChild(background);
        }
        else
        {
            root.SetChild(
                global::Gtk.Box.New(global::Gtk.Orientation.Vertical, 0));
        }
        
        var outer = global::Gtk.Box.New(global::Gtk.Orientation.Vertical, 10);
        outer.AddCssClass("dns-content");
        outer.SetMarginTop(22);
        outer.SetMarginBottom(22);
        outer.SetMarginStart(22);
        outer.SetMarginEnd(22);
        outer.SetHexpand(true);
        outer.SetVexpand(true);

        // The heading occupies only its own compact row.
        // The pig is positioned on the full-window overlay below, not here.
        var titleRow = global::Gtk.Box.New(global::Gtk.Orientation.Horizontal, 0);
        titleRow.SetSizeRequest(-1, 78);
        titleRow.SetHexpand(true);

        var title = global::Gtk.Label.New("Porkbun DynDns Updater");
        title.AddCssClass("dns-title");
        title.SetXalign(0);
        title.SetHalign(global::Gtk.Align.Start);
        title.SetValign(global::Gtk.Align.Start);
        title.SetMarginTop(14);
        titleRow.Append(title);
        outer.Append(titleRow);

        var form = global::Gtk.Grid.New();
        form.SetRowSpacing(10);
        form.SetColumnSpacing(12);
        form.AddCssClass("dns-form");
        outer.Append(form);

        _host = global::Gtk.Entry.New();
        _host.PlaceholderText = "e.g. home";
        _host.SetSizeRequest(220, 34);
        _host.SetHalign(global::Gtk.Align.Start);
        _domain = global::Gtk.Entry.New();
        _domain.PlaceholderText = "example.com";
        _domain.SetSizeRequest(220, 34);
        _domain.SetHalign(global::Gtk.Align.Start);

        _interval = global::Gtk.Scale.NewWithRange(global::Gtk.Orientation.Horizontal, 0, IntervalMinutes.Length - 1, 1);
        _interval.SetValue(9);
        _interval.SetDrawValue(false);
        _interval.SetSizeRequest(220, -1);
        _interval.SetHalign(global::Gtk.Align.Start);
        _intervalLabel = global::Gtk.Label.New("10 min");
        _intervalLabel.SetXalign(0);
        _intervalLabel.SetSizeRequest(85, -1);
        var intervalRow = global::Gtk.Box.New(global::Gtk.Orientation.Horizontal, 15);
        intervalRow.Append(_interval);
        intervalRow.Append(_intervalLabel);
        _interval.OnValueChanged += (_, _) =>
        {
            _intervalLabel.SetText(FormatDuration(GetIntervalMinutes()));
        };

        _ipv6 = global::Gtk.CheckButton.NewWithLabel("Also update IPv6 (AAAA)");
        _ipv6.SetCursorFromName("pointer");
        AddRow(form, 0, "Host", _host);
        AddRow(form, 1, "Domain", _domain);
        AddRow(form, 2, "Check interval", intervalRow);
        form.Attach(_ipv6, 1, 3, 1, 1);

        _ipv4Label = global::Gtk.Label.New("Not checked");
        _ipv6Label = global::Gtk.Label.New("Not checked");
        AddRow(form, 4, "Public IPv4", _ipv4Label);
        AddRow(form, 5, "Public IPv6", _ipv6Label);

        var actions = global::Gtk.Box.New(global::Gtk.Orientation.Horizontal, 8);
        actions.SetMarginTop(12);
        actions.SetMarginBottom(12);
        _start = global::Gtk.Button.NewWithLabel("Start updater");
        _stop = global::Gtk.Button.NewWithLabel("Stop updater");
        _start.SetCursorFromName("pointer");
        _stop.SetCursorFromName("pointer");
        _stop.SetVisible(false);
        actions.Append(_start);
        actions.Append(_stop);
        outer.Append(actions);

        var logTitle = global::Gtk.Label.New("Activity log");
        logTitle.SetXalign(0);
        logTitle.AddCssClass("dns-section-title");
        outer.Append(logTitle);
        // var note = global::Gtk.Label.New("Closing this GTK window exits the app. Tray support is not yet implemented.");
        // note.SetXalign(0);
        // note.SetWrap(true);
        // note.AddCssClass("dns-note");
        // outer.Append(note);

        var logView = global::Gtk.TextView.New();
        
        logView.SetLeftMargin(12);
        logView.SetRightMargin(12);
        logView.SetTopMargin(10);
        logView.SetBottomMargin(10);
        
        logView.SetEditable(false);
        logView.SetCursorVisible(false);
        logView.SetWrapMode(global::Gtk.WrapMode.WordChar);
        logView.AddCssClass("dns-log");
        _logBuffer = logView.GetBuffer();
        var scroller = global::Gtk.ScrolledWindow.New();
        scroller.AddCssClass("dns-log-scroller");
        // Let the log consume the remaining vertical space without forcing
        // its bottom border below the window.
        scroller.SetSizeRequest(-1, 80);
        scroller.SetChild(logView);
        scroller.SetVexpand(true);
        outer.Append(scroller);

        root.AddOverlay(outer);
        
        var pigPath = Path.Combine(
            AppContext.BaseDirectory, "Assets", "Justhead.png");

        if (File.Exists(pigPath))
        {
            // Scale the actual image data to 125x125 maximum.
            var pixbuf = global::GdkPixbuf.Pixbuf.NewFromFileAtScale(
                pigPath,
                125,
                125,
                true);

            var texture = global::Gdk.Texture.NewForPixbuf(pixbuf);

            var pig = global::Gtk.Image.NewFromPaintable(texture);

            pig.SetPixelSize(150);
            pig.SetHalign(global::Gtk.Align.End);
            pig.SetValign(global::Gtk.Align.Start);
            pig.SetMarginTop(12);
            form.SetColumnSpacing(12);
            pig.SetMarginEnd(22);
            pig.SetCanTarget(false);

            root.AddOverlay(pig);
        }

        _window.Child = root;
        _start.OnClicked += (_, _) => _ = StartAsync();
        _stop.OnClicked += (_, _) => _cts?.Cancel();
        _window.OnCloseRequest += (_, _) => { _cts?.Cancel(); return false; };
        AppendLog("Ready.");
    }
    
    private void ConfigureWindowIcon()
    {
        var iconDirectory = Path.Combine(
            AppContext.BaseDirectory, "Assets");

        var iconFile = Path.Combine(iconDirectory, "Justhead.png");

        if (!File.Exists(iconFile))
            return;

        var iconTheme = global::Gtk.IconTheme.GetForDisplay(
            _window.GetDisplay());

        iconTheme.AddSearchPath(iconDirectory);

        // GTK resolves Justhead to Justhead.png
        _window.SetIconName("Justhead");
    }
    
    private static int GetIntervalIndex(double value) =>
        Math.Clamp((int)Math.Round(value), 0, IntervalMinutes.Length - 1);

    private int GetIntervalMinutes() => IntervalMinutes[GetIntervalIndex(_interval.GetValue())];

    private static string FormatDuration(int minutes) => minutes < 60
        ? $"{minutes} min"
        : minutes % 60 == 0
            ? $"{minutes / 60} h"
            : $"{minutes / 60} h {minutes % 60} min";

    private static void ApplyStyles()
    {
        // GTK styling is applied through CSS, independently from the Avalonia theme.
        var provider = global::Gtk.CssProvider.New();
        provider.LoadFromString("""
            .dns-content { color: #edf5ff; font-size: 16px; }
            .dns-title { color: #a9d8f5; font-size: 27px; font-weight: 600; }
            .dns-section-title { font-weight: 600; }
            .dns-note { font-size: 14px; }
            .dns-log-scroller { background: rgba(0,0,0,0.22); border: 1px solid rgba(210,230,255,0.3); }
            .dns-log, .dns-log text { background: transparent; color: #edf5ff; }
            .dns-form entry { min-height: 34px; }
            """);
        var display = global::Gdk.Display.GetDefault();
        if (display is not null)
            global::Gtk.StyleContext.AddProviderForDisplay(display, provider, 800);
    }

    private static void AddRow(global::Gtk.Grid grid, int row, string label, global::Gtk.Widget input)
    {
        var caption = global::Gtk.Label.New(label);
        caption.SetSizeRequest(175, -1);
        caption.SetXalign(0);
        grid.Attach(caption, 0, row, 1, 1);
        if (input is global::Gtk.Label valueLabel) valueLabel.SetXalign(0);
        grid.Attach(input, 1, row, 1, 1);
    }

    private async Task StartAsync()
    {
        if (_runningTask is { IsCompleted: false }) return;
        var host = _host.GetText().Trim();
        var domain = _domain.GetText().Trim();
        var interval = GetIntervalMinutes();
        var includeIpv6 = _ipv6.GetActive();
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(domain))
        { AppendLog("Enter both hostname and domain."); return; }
        if (string.IsNullOrWhiteSpace(_config.PorkbunApiKey) || string.IsNullOrWhiteSpace(_config.PorkbunApiSecret))
        { AppendLog("Missing Porkbun credentials in appsettings.json."); return; }

        var record = new Record { HostName = host, Domain = domain };
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _start.SetVisible(false);
        _stop.SetVisible(true);
        _host.SetSensitive(false);
        _domain.SetSensitive(false);
        _interval.SetSensitive(false);
        _ipv6.SetSensitive(false);

        var status = new InlineProgress<StatusReport>(r => PostUi(() => AppendLog(r.Content)));

        var progress = new InlineProgress<ProgressReport>(r => PostUi(() =>
        {
            if (r.IP != null)
            {
                if (r.DnsType == DnsType.A) _ipv4Label.SetText(r.IP);
                else _ipv6Label.SetText(r.IP);
            }
        }));
        _runningTask = UpdateAsync();
        try { await _runningTask; }
        catch (OperationCanceledException) { AppendLog("Updater stopped."); }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
        finally
        {
            _start.SetVisible(true);
            _stop.SetVisible(false);
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
            AppendLog($"Monitoring every {interval} minute(s).");
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
        _logBuffer.GetEndIter(out var end);
        _logBuffer.Insert(end, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}\n", -1);
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;
        public InlineProgress(Action<T> callback) => _callback = callback;
        public void Report(T value) => _callback(value);
    }
}
