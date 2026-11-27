#!/usr/bin/env bash
# Builds an AppImage from a self-contained linux-x64 publish directory.
# Downloads appimagetool on first use (set APPIMAGETOOL to override).
# Usage: VERSION=1.1.0 PUBLISH=/path/to/publish OUT=/path/to/dist ./build-appimage.sh
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../../.." && pwd)"
VERSION="${VERSION:-1.1.0}"
PUBLISH="${PUBLISH:-$ROOT/cross/Sc880.Desktop/bin/Release/net8.0/linux-x64/publish}"
OUT="${OUT:-$ROOT/dist/linux}"
TOOL="${APPIMAGETOOL:-$OUT/appimagetool-x86_64.AppImage}"

if [ ! -x "$PUBLISH/BraubergSC880" ]; then
  echo "error: publish directory '$PUBLISH' does not contain BraubergSC880" >&2
  exit 1
fi

mkdir -p "$OUT"
APPDIR="$(mktemp -d)"
chmod 755 "$APPDIR"
trap 'rm -rf "$APPDIR" "$OUT/.appimage-work"' EXIT

mkdir -p "$APPDIR/usr/lib/brauberg-sc880"
cp -a "$PUBLISH/." "$APPDIR/usr/lib/brauberg-sc880/"
find "$APPDIR/usr/lib/brauberg-sc880" -type d -exec chmod 755 {} +
find "$APPDIR/usr/lib/brauberg-sc880" -type f -exec chmod 644 {} +
chmod 755 "$APPDIR/usr/lib/brauberg-sc880/BraubergSC880"
install -m 644 "$HERE/brauberg-sc880.desktop" "$APPDIR/brauberg-sc880.desktop"
install -m 644 "$HERE/brauberg-sc880.png" "$APPDIR/brauberg-sc880.png"
ln -s brauberg-sc880.png "$APPDIR/.DirIcon"

cat > "$APPDIR/AppRun" <<'APPRUN'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/lib/brauberg-sc880/BraubergSC880" "$@"
APPRUN
chmod 755 "$APPDIR/AppRun"

if [ ! -x "$TOOL" ]; then
  echo "downloading appimagetool..."
  curl -fsSL -o "$TOOL" \
    "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
  chmod 755 "$TOOL"
fi

TARGET="$OUT/brauberg-sc880-$VERSION-x86_64.AppImage"
rm -f "$TARGET"
export ARCH=x86_64
if ! "$TOOL" "$APPDIR" "$TARGET" 2>/tmp/brauberg-appimage.log; then
  # FUSE is often unavailable in containers; use the tool's own extract mode.
  WORK="$OUT/.appimage-work"
  mkdir -p "$WORK"
  ( cd "$WORK" && "$TOOL" --appimage-extract >/dev/null 2>&1 )
  ARCH=x86_64 "$WORK/squashfs-root/AppRun" "$APPDIR" "$TARGET" >/tmp/brauberg-appimage.log 2>&1 || {
    cat /tmp/brauberg-appimage.log >&2
    exit 1
  }
fi
chmod 755 "$TARGET"
echo "built $TARGET"
