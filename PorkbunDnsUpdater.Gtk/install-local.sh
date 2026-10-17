#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$HOME/.local/opt/porkbun-dns-updater"
mkdir -p "$TARGET" "$HOME/.local/share/applications"
dotnet publish "$HERE/PorkbunDnsUpdater.Gtk.csproj" -c Release -r linux-x64 --self-contained false -o "$TARGET"
cat > "$HOME/.local/share/applications/porkbun-dns-updater.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Porkbun DNS Updater
Comment=Update Porkbun dynamic DNS records
Exec=$TARGET/PorkbunDnsUpdater.Gtk
Icon=network-server
Terminal=false
Categories=Network;Utility;
EOF
chmod +x "$TARGET/PorkbunDnsUpdater.Gtk"
printf 'Installed. Configure credentials in your user environment and launch Porkbun DNS Updater.\n'
