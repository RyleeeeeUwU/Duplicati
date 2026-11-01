#!/usr/bin/env bash
# Builds a Release zip for each supported game version.
# Needs BeatSaberDir set (Duplicati.csproj.user, or BEAT_SABER_DIR in the environment).
set -euo pipefail

cd "$(dirname "$0")/.."

# Don't also copy into the game's Plugins folder while packaging.
for version in 1.39.1 1.40.8; do
  echo "==> Packaging for Beat Saber $version"
  dotnet build Duplicati/Duplicati.csproj -c Release --no-incremental \
    -p:GameVersion="$version" -p:CopyToPlugins=false
done

echo
echo "Zips:"
ls -1 Duplicati/bin/Release/net472/zip/*.zip
