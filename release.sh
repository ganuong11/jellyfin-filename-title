#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

DLL="FilenameTitlePlugin/bin/Release/net8.0/Jellyfin.Plugin.FilenameTitlePlugin.dll"
ZIP="FilenameTitlePlugin.zip"

rm -rf FilenameTitlePlugin/bin/Release
dotnet build -c Release FilenameTitlePlugin/FilenameTitlePlugin.csproj
zip -j "$ZIP" "$DLL"
echo "Release: $ZIP"
