#!/usr/bin/env bash
# Builds a Debian package from a self-contained linux-x64 publish directory.
# Usage: VERSION=1.1.0 PUBLISH=/path/to/publish OUT=/path/to/dist ./build-deb.sh
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../../.." && pwd)"
VERSION="${VERSION:-1.1.0}"
PUBLISH="${PUBLISH:-$ROOT/cross/Sc880.Desktop/bin/Release/net8.0/linux-x64/publish}"
OUT="${OUT:-$ROOT/dist/linux}"

if [ ! -x "$PUBLISH/BraubergSC880" ]; then
  echo "error: publish directory '$PUBLISH' does not contain BraubergSC880" >&2
  exit 1
fi

mkdir -p "$OUT"
ROOTDIR="$(mktemp -d)"
trap 'rm -rf "$ROOTDIR"' EXIT

mkdir -p "$ROOTDIR/DEBIAN"
mkdir -p "$ROOTDIR/usr/lib/brauberg-sc880"
mkdir -p "$ROOTDIR/usr/bin"
mkdir -p "$ROOTDIR/usr/share/applications"
mkdir -p "$ROOTDIR/usr/share/icons/hicolor/256x256/apps"

cp -a "$PUBLISH/." "$ROOTDIR/usr/lib/brauberg-sc880/"
find "$ROOTDIR/usr/lib/brauberg-sc880" -type d -exec chmod 755 {} +
find "$ROOTDIR/usr/lib/brauberg-sc880" -type f -exec chmod 644 {} +
chmod 755 "$ROOTDIR/usr/lib/brauberg-sc880/BraubergSC880"
ln -s ../lib/brauberg-sc880/BraubergSC880 "$ROOTDIR/usr/bin/brauberg-sc880"
install -m 644 "$HERE/brauberg-sc880.desktop" "$ROOTDIR/usr/share/applications/brauberg-sc880.desktop"
install -m 644 "$HERE/brauberg-sc880.png" "$ROOTDIR/usr/share/icons/hicolor/256x256/apps/brauberg-sc880.png"

cat > "$ROOTDIR/DEBIAN/control" <<CONTROL
Package: brauberg-sc880
Version: $VERSION
Section: utils
Priority: optional
Architecture: amd64
Maintainer: mirip666 <https://github.com/mirip666>
Installed-Size: $(du -sk "$ROOTDIR/usr" | cut -f1)
Depends: libx11-6, libice6, libsm6, libfontconfig1, libfreetype6, libssl3 | libssl1.1, libgl1
Homepage: https://github.com/mirip666/brauberg-sc880
Description: BRAUBERG SC-880 scientific calculator
 Native cross-platform reimplementation of the BRAUBERG SC-880
 scientific calculator. Supports complex numbers, matrices, vectors,
 statistics, equations and unit conversions.
CONTROL

dpkg-deb --build --root-owner-group "$ROOTDIR" "$OUT/brauberg-sc880_${VERSION}_amd64.deb" >/dev/null
echo "built $OUT/brauberg-sc880_${VERSION}_amd64.deb"
