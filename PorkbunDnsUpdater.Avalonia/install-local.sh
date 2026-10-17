#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
TARGET="$HOME/.local/opt/porkbun-dns-updater-avalonia"
mkdir -p "$TARGET" "$HOME/.local/share/applications"
dotnet publish -c Release -r linux-x64 --self-contained true -o "$TARGET"
cat > "$HOME/.local/share/applications/porkbun-dns-updater-avalonia.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Porkbun DNS Updater (Avalonia)
Comment=Dynamic DNS update tool
Exec=$TARGET/PorkbunDnsUpdater.Avalonia
Icon=network-server
Terminal=false
Categories=Network;Utility;
EOF
chmod +x "$TARGET/PorkbunDnsUpdater.Avalonia"
echo "Installed. Launch from KDE menu as Porkbun DNS Updater (Avalonia)."
