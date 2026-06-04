#!/usr/bin/env bash
set -e

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <version>" >&2
  echo "Example: $0 1.1.3" >&2
  exit 1
fi

cd "$(dirname "$0")"

VERSION="$1"
ASSEMBLY_VERSION="${VERSION}.0"
PLUGIN_DIR="/var/lib/jellyfin/plugins/Filename Title_${VERSION}"
DLL="/Download/title/FilenameTitlePlugin/bin/Release/net8.0/Jellyfin.Plugin.FilenameTitlePlugin.dll"

sed -i '' "s|<Version>[0-9.]*</Version>|<Version>${ASSEMBLY_VERSION}</Version>|" \
  FilenameTitlePlugin/FilenameTitlePlugin.csproj

dotnet build -c Release FilenameTitlePlugin/FilenameTitlePlugin.csproj

sudo mkdir -p "$PLUGIN_DIR"
sudo cp "$DLL" "$PLUGIN_DIR/"
sudo tee "$PLUGIN_DIR/meta.json" > /dev/null << EOF
{
  "category": "Metadata",
  "changelog": "Initial release",
  "description": "Sets item titles from cleaned-up filenames when no metadata provider has set a title",
  "guid": "3f2a1b4c-5d6e-7f8a-9b0c-1d2e3f4a5b6c",
  "name": "Filename Title",
  "overview": "Derive item titles from filenames",
  "owner": "local",
  "targetAbi": "10.9.0.0",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%S.0000000Z)",
  "version": "${VERSION}",
  "status": "Active",
  "autoUpdate": false,
  "assemblies": []
}
EOF
sudo chown -R jellyfin:jellyfin "$PLUGIN_DIR"
sudo systemctl restart jellyfin
sudo journalctl -u jellyfin -n 30 | grep -i "plugin\|filename\|error"
