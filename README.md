# Porkbun DynDns Updater

Desktop dynamic DNS updater for [Porkbun](https://porkbun.com/). The solution contains one shared .NET library and three different user interfaces.

## Projects

| Project | Purpose | Platform |
|---|---|---|
| `PorkbunDnsUpdater.Core` | Porkbun HTTP client, DNS record handling, IP monitoring and shared models | .NET 10 library |
| `PorkbunDnsUpdater.Avalonia` | Cross-platform GUI, system tray and activity log | Windows and Linux |
| `PorkbunDnsUpdater.Gtk` | GTK 4 proof-of-concept GUI | Linux (GTK 4 required) |
| `PorkbunDnsUpdater.Wpf` | Original GUI; currently has its own backend classes | Windows only |

All four projects target **.NET 10** (`net10.0` or WPF's `net10.0-windows`). WPF is not currently wired to Core; changes in Core will not automatically affect WPF.

## Features and behavior

- Monitors a specified host/domain and updates a Porkbun **A (IPv4)** record when the public IP changes.
- Optional **AAAA (IPv6)** updates in the Avalonia and GTK interfaces.
- Initializes the current public IP and DNS record, then polls periodically and logs status.
- Avalonia offers a non-linear interval selector, with values from **1 minute to 24 hours**, separate IPv4/IPv6 status labels, and system-tray integration.

This application **modifies live DNS records**. Test with a non-critical record and review your Porkbun API permissions before using it.

## Getting started

Install a **.NET 10 SDK** for development. For Linux, GTK additionally requires the GTK 4 native libraries. From the solution root:

```bash
dotnet restore PorkbunDnsUpdater.Avalonia/PorkbunDnsUpdater.Avalonia.csproj
dotnet build PorkbunDnsUpdater.Avalonia/PorkbunDnsUpdater.Avalonia.csproj
dotnet run --project PorkbunDnsUpdater.Avalonia/PorkbunDnsUpdater.Avalonia.csproj
```

For GTK (Linux):

```bash
sudo pacman -S gtk4   # CachyOS / Arch family
dotnet run --project PorkbunDnsUpdater.Gtk/PorkbunDnsUpdater.Gtk.csproj
```

For WPF (Windows):

```powershell
dotnet build PorkbunDnsUpdater.Wpf/PorkbunDnsUpdater.csproj
```

Avoid building the whole solution on Linux unless you have deliberately configured Windows targeting: WPF is Windows-specific.

## Configuration

**Avalonia:** The application reads `PorkbunDnsUpdater.Avalonia/appsettings.json` (copied to the build/publish output). Create it from the included `appsettings.example.json` and enter your own Porkbun credentials. It is loaded when the app starts; changes require a restart.

**WPF:** The independent WPF implementation reads `PorkbunDnsUpdater.Wpf/App.config` through `ConfigurationManager.AppSettings`.

**GTK:** The current `Program.cs` constructs an empty `AppConfig` and does not read API credentials from JSON or environment variables. **Credential setup for GTK is incomplete**; update its configuration loading before attempting real DNS updates. See the GTK README.

Never commit API credentials, share an archive containing them, or publish them as part of a distributable application. The current Avalonia project copies `appsettings.json` to publish output, so publishing with live credentials requires additional care. The example file contains placeholders only.

## Further documentation

- [Avalonia application](PorkbunDnsUpdater.Avalonia/README.md)
- [GTK 4 application](PorkbunDnsUpdater.Gtk/README.md)
- [WPF application](PorkbunDnsUpdater.Wpf/README.md)
- [Technical review](REVIEW_FINDINGS.md)

## Current limitations

- No automated test project is present in the uploaded solution.
- The Core DNS service polls with a one-second delay loop and can throw on an empty DNS record list; see the review for details.
- Release builds and GUI behavior were not verified in this review environment, which lacks the .NET SDK.
