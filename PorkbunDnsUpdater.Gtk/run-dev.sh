#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ ! -f appsettings.json ]]; then
    echo 'Missing appsettings.json. Copy appsettings.example.json to appsettings.json and configure credentials.' >&2
    exit 1
fi
exec dotnet run --project PorkbunDnsUpdater.Gtk.csproj
