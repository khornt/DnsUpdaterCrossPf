# Porkbun DNS Updater — Avalonia UI

This is a **third desktop UI** using the existing `PorkbunDnsUpdater.Core` service. The WPF and GTK source projects are retained as provided in the prior conversion.

## Features
- Native Avalonia desktop window on Windows and Linux
- Host / Domain / polling interval / IPv6 inputs
- Start / Stop updater, public IP labels, timestamped activity log
- **System tray**: closing the window hides it (does not stop DNS updates); clicking tray icon or choosing **Show window** restores it; choose **Quit** to terminate
- Regular OS minimize button minimizes to taskbar; closing hides to tray

## Linux (CachyOS)

Install .NET 9 SDK (`dotnet-sdk` package or equivalent), then from the project folder:

```bash
./run-dev.sh
```

To install a clickable KDE application launcher:

```bash
./install-local.sh
```

The publish script is for Linux x64. GTK is **not** required for the Avalonia version.

## Windows
Open `PorkbunDnsUpdater.Avalonia.csproj` in Rider and run it with the .NET SDK. No WPF dependencies are needed for this project.

## Porkbun credentials

Set `PORKBUN_API_KEY` and `PORKBUN_API_SECRET` as process environment variables, and optionally `PORKBUN_API_URL` / `PORKBUN_API_URL_V4`. **Do not put real credentials into source-controlled files.** If launched from KDE rather than a terminal, environment variables must be available to the desktop session (a terminal-only `export` is not enough). These credentials are read when the app starts.

## Platform notes

Avalonia TrayIcon is officially supported on Windows and on Linux desktops implementing StatusNotifierItem or AppIndicator. KDE Plasma generally supports this. The tray icon and interactions still need functional verification on your specific CachyOS system.

## Limitations
- Source is prepared but **not build-verified** in this environment (no `dotnet` SDK available).
- Core is shared with GTK. The WPF project is retained in its prior form and still contains duplicated business classes; cleanup is pending as previously agreed.
- Quit cancels the monitoring token, then terminates the app; it is not a long-running background daemon.
