#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$HOME/.local/opt/porkbun-dns-updater"
if [[ ! -f "$HERE/appsettings.json" ]]; then
    echo 'Missing appsettings.json. Copy appsettings.example.json to appsettings.json and configure credentials.' >&2
    exit 1
fi
mkdir -p "$TARGET" "$HOME/.local/share/applications"
dotnet publish "$HERE/PorkbunDnsUpdater.Gtk.csproj" -c Release -r linux-x64 --self-contained false -o "$TARGET"
chmod 600 "$TARGET/appsettings.json"
cat > "$HOME/.local/share/applications/porkbun-dns-updater.desktop" <<EOF_DESKTOP
[Desktop Entry]
Type=Application
Name=Porkbun DNS Updater
Comment=Update Porkbun dynamic DNS records
Exec=$TARGET/PorkbunDnsUpdater.Gtk
Icon=network-server
Terminal=false
Categories=Network;Utility;
EOF_DESKTOP
chmod +x "$TARGET/PorkbunDnsUpdater.Gtk"
printf 'Installed. Configuration: %s/appsettings.json\n' "$TARGET"
