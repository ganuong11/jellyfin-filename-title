#!/usr/bin/env bash
set -e

cd "$(dirname "$0")"

CSPROJ="FilenameTitlePlugin/FilenameTitlePlugin.csproj"
CURRENT_VERSION=$(grep -oE '<Version>[0-9.]+</Version>' "$CSPROJ" | sed -E 's|<Version>([0-9.]+)</Version>|\1|')

print_usage() {
  echo "Usage: $0 [<version>]"
  echo "If <version> is omitted, increments the csproj's current version by 1 (3rd component)."
  echo "Example: $0 1.1.4"
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  print_usage
  exit 0
fi

if [[ $# -gt 1 ]]; then
  print_usage >&2
  exit 1
fi

if [[ $# -eq 0 ]]; then
  IFS='.' read -r MAJOR MINOR BUILD _ <<< "$CURRENT_VERSION"
  VERSION="${MAJOR}.${MINOR}.$((BUILD + 1))"
else
  VERSION="$1"
fi

ASSEMBLY_VERSION="${VERSION}.0"
PLUGIN_DIR="/var/lib/jellyfin/plugins/Filename Title_${VERSION}"
DLL="/Download/title/FilenameTitlePlugin/bin/Release/net8.0/Jellyfin.Plugin.FilenameTitlePlugin.dll"

echo "Installing Filename Title ${VERSION} (assembly ${ASSEMBLY_VERSION})"

sed -i '' "s|<Version>[0-9.]*</Version>|<Version>${ASSEMBLY_VERSION}</Version>|" "$CSPROJ"

dotnet build -c Release "$CSPROJ"

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
