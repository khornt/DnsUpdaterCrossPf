#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ -f ../local-credentials.sh ]]; then source ../local-credentials.sh; fi
exec dotnet run --project PorkbunDnsUpdater.Gtk.csproj
