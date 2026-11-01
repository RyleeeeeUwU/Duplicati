#!/usr/bin/env bash
# Builds a Release zip per Beat Saber version.
#
#   scripts/package.sh                    # default versions (see DEFAULT_VERSIONS)
#   scripts/package.sh 1.44.1             # just that version
#   scripts/package.sh 1.39.1 1.40.8      # several
#   scripts/package.sh -p:GameVersion=1.44.1   # also accepted
#
# Needs BeatSaberDir set (Duplicati.csproj.user, or BEAT_SABER_DIR in the environment).
set -euo pipefail

cd "$(dirname "$0")/.."

DEFAULT_VERSIONS=(1.39.1 1.40.8)

versions=()
for arg in "$@"; do
  case "$arg" in
    -p:GameVersion=*|-property:GameVersion=*) versions+=("${arg#*=}") ;;
    [0-9]*.[0-9]*.[0-9]*)                     versions+=("$arg") ;;
    *) echo "Unrecognised argument: $arg" >&2
       echo "Usage: $0 [version ...]   e.g. $0 1.44.1" >&2
       exit 1 ;;
  esac
done

if [ ${#versions[@]} -eq 0 ]; then
  versions=("${DEFAULT_VERSIONS[@]}")
fi

zip_dir=Duplicati/bin/Release/net472/zip
built=()

# CopyToPlugins=false: don't also copy into the game's Plugins folder while packaging.
for version in "${versions[@]}"; do
  echo "==> Packaging for Beat Saber $version"
  dotnet build Duplicati/Duplicati.csproj -c Release --no-incremental \
    -p:GameVersion="$version" -p:CopyToPlugins=false
  built+=("$(ls -1 "$zip_dir"/*-bs"$version".zip)")
done

echo
echo "Built:"
printf '  %s\n' "${built[@]}"
