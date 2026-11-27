#!/usr/bin/env bash
# Builds a portable tar.gz from a self-contained linux-x64 publish directory.
# Usage: VERSION=1.1.0 PUBLISH=/path/to/publish OUT=/path/to/dist ./build-tar.sh
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../../.." && pwd)"
VERSION="${VERSION:-1.1.0}"
PUBLISH="${PUBLISH:-$ROOT/cross/Sc880.Desktop/bin/Release/net8.0/linux-x64/publish}"
OUT="${OUT:-$ROOT/dist/linux}"

if [ ! -x "$PUBLISH/BraubergSC880" ]; then
  echo "error: publish directory '$PUBLISH' does not contain BraubergSC880" >&2
  echo "run: dotnet publish cross/Sc880.Desktop -c Release -r linux-x64 --self-contained true -o $PUBLISH" >&2
  exit 1
fi

mkdir -p "$OUT"
NAME="brauberg-sc880-$VERSION"
STAGE="$OUT/$NAME"
rm -rf "$STAGE"
mkdir -p "$STAGE"

cp -a "$PUBLISH/." "$STAGE/"
cp "$ROOT/README.md" "$STAGE/README.md"
cp "$ROOT/LICENSE" "$STAGE/LICENSE"
install -m 644 "$HERE/brauberg-sc880.desktop" "$STAGE/brauberg-sc880.desktop"
install -m 644 "$HERE/brauberg-sc880.png" "$STAGE/brauberg-sc880.png"

find "$STAGE" -type d -exec chmod 755 {} +
find "$STAGE" -type f -exec chmod 644 {} +
chmod 755 "$STAGE/BraubergSC880"

cat > "$STAGE/brauberg-sc880" <<'LAUNCHER'
#!/bin/sh
exec "$(dirname "$(readlink -f "$0")")/BraubergSC880" "$@"
LAUNCHER
chmod 755 "$STAGE/brauberg-sc880"

tar -C "$OUT" -czf "$OUT/$NAME-linux-x64.tar.gz" "$NAME"
rm -rf "$STAGE"
echo "built $OUT/$NAME-linux-x64.tar.gz"
