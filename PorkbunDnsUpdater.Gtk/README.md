# PorkbunDnsUpdater.Gtk

GTK 4/.NET 10 frontend for the shared `PorkbunDnsUpdater.Core` service, styled after the Avalonia frontend.

## Configuration

Copy `appsettings.example.json` to `appsettings.json` in this project and supply API credentials. Configuration is loaded from the output directory using `Microsoft.Extensions.Configuration` (the same scheme as Avalonia). Never commit or distribute real credentials.

## UI

- `Assets/background.jpg` and `Assets/Justhead.png` are copied to the output directory.
- Host and Domain are separate short entry fields.
- Nonlinear interval scale: 1–10 minutes, 15/20/30/45/60/90/120/180/240/360/480/720/1440 minutes; 10-minute default.
- Optional IPv6 (AAAA) and separate IPv4/IPv6 status indicators.
- Start and Stop toggle visibility; Stop requests cancellation through `CancellationTokenSource`.
- Activity log with a translucent background and an informational close notice.
- GTK CSS adjusts title color, font sizes, and controls; native window decorations may differ from Avalonia.

**Important:** This GTK frontend does **not yet have system-tray integration**. Closing its window exits the app and cancels its updater. Do not copy Avalonia's “closing hides to tray” promise until GTK supports it.

## Build

```bash
dotnet restore PorkbunDnsUpdater.Gtk/PorkbunDnsUpdater.Gtk.csproj
dotnet build PorkbunDnsUpdater.Gtk/PorkbunDnsUpdater.Gtk.csproj
dotnet run --project PorkbunDnsUpdater.Gtk/PorkbunDnsUpdater.Gtk.csproj
```

## Status

This is a source-level GTK 4/GirCore UI port and has not been compiled or tested in the current environment. Verify GTK rendering on CachyOS and stop/cancel behavior before relying on it. The `Program.cs` startup is from the prior GTK JSON configuration refactor. GUI callbacks and background continuations still warrant a thread-affinity test on GTK; changes to `Core` are out of scope.
