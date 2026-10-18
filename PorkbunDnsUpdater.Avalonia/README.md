# Avalonia interface

Cross-platform graphical interface for `PorkbunDnsUpdater.Core`. Targets **.NET 10** and uses Avalonia 12.1.4 in this archive.

## Features

- Separate Host and Domain inputs.
- **Non-linear check interval** with selectable values from 1 minute to 24 hours, default 10 minutes.
- Optional AAAA (IPv6) monitoring and distinct current IPv4/IPv6 status labels.
- Start and Stop buttons switch visibility when monitoring state changes.
- Timestamped activity log, with translucent background, bundled background and pig-head artwork.
- **Tray operation:** closing the main window hides it without stopping monitoring. Use the tray icon or **Show window** to restore it; **Quit** stops the updater and exits.

The normal OS minimize control minimizes the window; it does not invoke the application's close-to-tray behavior.

## Configuration

Create `appsettings.json` in this project directory, using `appsettings.example.json` in the solution root as a template. Fill in the `Porkbun` API key and secret. The executable reads it from its output directory at startup using `Microsoft.Extensions.Configuration.Json` and `.Binder`.

**Security:** `appsettings.json` is copied to build **and publish** output. Never distribute a package containing your real credentials. Exclude your local file from version control. Prefer user-specific configuration storage for production use.

## Run (Windows or Linux)

```bash
dotnet run --project PorkbunDnsUpdater.Avalonia/PorkbunDnsUpdater.Avalonia.csproj
```

From the Avalonia project directory on Linux, `./run-dev.sh` performs the same operation. GTK is not needed for the Avalonia GUI.

On Linux x64, `./install-local.sh` publishes a **self-contained** application to `~/.local/opt/porkbun-dns-updater-avalonia` and creates a desktop launcher in `~/.local/share/applications`. It does **not** configure or protect API credentials.

System-tray behavior depends on Linux desktop support for a compatible status notifier; test on your own desktop environment.

## Notes

The application depends on the shared Core library, not on the WPF backend. `Stop updater` cancels the update token; tray **Quit** requests cancellation and shuts down the application. Graceful completion of outstanding HTTP operations is not awaited in `App.Quit`.
