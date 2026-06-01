#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [[ $# -lt 1 || $# -gt 2 ]]; then
  echo "Usage: $0 <version> [<previous-tag>]" >&2
  echo "Example: $0 1.1.3.0 v1.1.2" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "Error: 'jq' is required but not installed." >&2
  exit 1
fi

VERSION="$1"
PREV_TAG="${2:-}"

DLL="FilenameTitlePlugin/bin/Release/net8.0/Jellyfin.Plugin.FilenameTitlePlugin.dll"
ZIP="FilenameTitlePlugin.zip"

rm -rf FilenameTitlePlugin/bin/Release
dotnet build -c Release FilenameTitlePlugin/FilenameTitlePlugin.csproj
zip -j "$ZIP" "$DLL"

if command -v md5sum >/dev/null 2>&1; then
  CHECKSUM="$(md5sum "$ZIP" | awk '{print $1}')"
else
  CHECKSUM="$(md5 -q "$ZIP")"
fi

TIMESTAMP="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
TARGET_ABI="10.9.0.0"
SOURCE_URL="https://github.com/ganuong11/jellyfin-filename-title/releases/download/v${VERSION}/Jellyfin.Plugin.FilenameTitlePlugin.zip"

if [[ -n "$PREV_TAG" ]]; then
  git rev-parse --verify "$PREV_TAG" >/dev/null
  CHANGELOG="$(git log "${PREV_TAG}..HEAD" --pretty=format:'%s' | paste -sd ';' -)"
else
  CHANGELOG="$(git log -1 --pretty=format:'%s')"
fi

TMP="$(mktemp)"
jq -n --arg category "Metadata" \
      --arg description "Sets item titles from cleaned-up filenames when no metadata provider has set a title." \
      --arg guid "3f2a1b4c-5d6e-7f8a-9b0c-1d2e3f4a5b6c" \
      --arg imageUrl "" \
      --arg name "Filename Title" \
      --arg overview "Derive item titles from filenames" \
      --arg owner "adielsa" \
      --arg changelog "$CHANGELOG" \
      --arg checksum "$CHECKSUM" \
      --arg sourceUrl "$SOURCE_URL" \
      --arg targetAbi "$TARGET_ABI" \
      --arg timestamp "$TIMESTAMP" \
      --arg version "$VERSION" \
      '[
         {
           category: $category,
           description: $description,
           guid: $guid,
           imageUrl: $imageUrl,
           name: $name,
           overview: $overview,
           owner: $owner,
           versions: [
             {
               changelog: $changelog,
               checksum: $checksum,
               sourceUrl: $sourceUrl,
               targetAbi: $targetAbi,
               timestamp: $timestamp,
               version: $version
             }
           ]
         }
       ]' > "$TMP"
mv "$TMP" manifest.json

echo "Released v${VERSION} — checksum ${CHECKSUM}"
echo "Source: ${SOURCE_URL}"
