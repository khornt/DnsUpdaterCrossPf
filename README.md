# Porkbun DNS Updater — GTK 4 Linux port (proof of concept)

The archive includes the original WPF application under `PorkbunDnsUpdater.Wpf` (Windows), a cross-platform library under `PorkbunDnsUpdater.Core`, and a GTK 4 app under `PorkbunDnsUpdater.Gtk` (Linux).

## What works in the GTK project
- Enter host/domain, select monitoring interval, toggle IPv6, Start/Stop.
- Calls the original Porkbun API client and DNS updating service, including initial read/update and continuous polling.
- Displays current IPv4/IPv6, progress log.
- Normal desktop minimize keeps the process alive; closing the window stops the process. No tray support.

## Start on CachyOS
Install GTK 4 and .NET SDK 9 or a compatible newer SDK that supports targeting net9.0:

```bash
sudo pacman -S gtk4 dotnet-sdk
```

In Rider open the project folder or GTK `.csproj`, restore NuGet packages, and run `PorkbunDnsUpdater.Gtk`. Or:

```bash
cp local-credentials.example.sh local-credentials.sh
# edit local-credentials.sh with NEW Porkbun API credentials
./PorkbunDnsUpdater.Gtk/run-dev.sh
```

You can alternatively set `PORKBUN_API_KEY` and `PORKBUN_API_SECRET` in Rider's run configuration. The source archive contains **no uploaded live secrets**. Credentials must be available in the process environment on every launch. For the desktop launcher, configure them through an appropriate local startup wrapper or session environment before launch; the install script does not embed credentials.

## KDE application launcher

```bash
./PorkbunDnsUpdater.Gtk/install-local.sh
```

This publishes a framework-dependent executable to `~/.local/opt/porkbun-dns-updater/` and installs a KDE-visible desktop launcher. GTK 4 and the matching .NET runtime must be installed. Set credentials in the desktop session environment before starting it.

## Limitations and verification
- The GTK UI is a fresh translation, not a pixel-identical WPF reproduction.
- It uses GirCore.Gtk-4.0 0.8.1. The container does not include a .NET SDK or graphical desktop, so it was not compiled/runtime tested. GTK API adjustments may be needed when built in Rider.
- This is a proof of concept; DNS changes are real when API credentials are configured. Test with a non-critical DNS record first.
- The WPF project is retained independently, not yet converted to consume Core (to minimize regression risk).
- The original ZIP included plaintext Porkbun credentials. Rotate them. The archived WPF `App.config` has the credential values blanked; the Core reads environment variables.
