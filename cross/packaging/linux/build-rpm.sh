#!/usr/bin/env bash
# Builds an RPM package from a self-contained linux-x64 publish directory.
# Requires rpmbuild (Fedora/RHEL: rpm-build; Debian/Ubuntu: rpm).
# Usage: VERSION=1.1.0 PUBLISH=/path/to/publish OUT=/path/to/dist ./build-rpm.sh
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../../.." && pwd)"
VERSION="${VERSION:-1.1.0}"
PUBLISH="${PUBLISH:-$ROOT/cross/Sc880.Desktop/bin/Release/net8.0/linux-x64/publish}"
OUT="${OUT:-$ROOT/dist/linux}"

command -v rpmbuild >/dev/null 2>&1 || { echo "error: rpmbuild not found" >&2; exit 1; }
if [ ! -x "$PUBLISH/BraubergSC880" ]; then
  echo "error: publish directory '$PUBLISH' does not contain BraubergSC880" >&2
  exit 1
fi

mkdir -p "$OUT"
TOPDIR="$(mktemp -d)"
trap 'rm -rf "$TOPDIR"' EXIT
mkdir -p "$TOPDIR/BUILD" "$TOPDIR/RPMS" "$TOPDIR/SOURCES" "$TOPDIR/SPECS" "$TOPDIR/SRPMS"

APPSTAGE="$TOPDIR/appstage"
mkdir -p "$APPSTAGE/app"
cp -a "$PUBLISH/." "$APPSTAGE/app/"
find "$APPSTAGE/app" -type d -exec chmod 755 {} +
find "$APPSTAGE/app" -type f -exec chmod 644 {} +
chmod 755 "$APPSTAGE/app/BraubergSC880"
install -m 644 "$HERE/brauberg-sc880.desktop" "$APPSTAGE/app/brauberg-sc880.desktop"
install -m 644 "$HERE/brauberg-sc880.png" "$APPSTAGE/app/brauberg-sc880.png"
tar -C "$APPSTAGE" -czf "$TOPDIR/SOURCES/app.tar.gz" app

cat > "$TOPDIR/SPECS/brauberg-sc880.spec" <<SPEC
%global __requires_exclude ^libmscordaccore\\.so.*$

Name:           brauberg-sc880
Version:        $VERSION
Release:        1%{?dist}
Summary:        BRAUBERG SC-880 scientific calculator
License:        MIT
URL:            https://github.com/mirip666/brauberg-sc880
Source0:        app.tar.gz
BuildArch:      x86_64
Requires:       openssl

%description
Native cross-platform reimplementation of the BRAUBERG SC-880 scientific
calculator. Supports complex numbers, matrices, vectors, statistics,
equations and unit conversions.

%prep
%setup -q -n app

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}/usr/lib/brauberg-sc880
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps
cp -a . %{buildroot}/usr/lib/brauberg-sc880/
chmod 755 %{buildroot}/usr/lib/brauberg-sc880/BraubergSC880
rm -f %{buildroot}/usr/lib/brauberg-sc880/brauberg-sc880.desktop %{buildroot}/usr/lib/brauberg-sc880/brauberg-sc880.png
ln -s ../lib/brauberg-sc880/BraubergSC880 %{buildroot}/usr/bin/brauberg-sc880
install -m 644 brauberg-sc880.desktop %{buildroot}/usr/share/applications/brauberg-sc880.desktop
install -m 644 brauberg-sc880.png %{buildroot}/usr/share/icons/hicolor/256x256/apps/brauberg-sc880.png

%files
/usr/lib/brauberg-sc880
/usr/bin/brauberg-sc880
/usr/share/applications/brauberg-sc880.desktop
/usr/share/icons/hicolor/256x256/apps/brauberg-sc880.png

%changelog
* Thu Nov 26 2026 mirip666 - $VERSION-1
- Native Linux port based on Avalonia
SPEC

rpmbuild --define "_topdir $TOPDIR" --define "_build_id_links none" -bb "$TOPDIR/SPECS/brauberg-sc880.spec" >/tmp/brauberg-rpmbuild.log 2>&1 || {
  cat /tmp/brauberg-rpmbuild.log >&2
  exit 1
}

RPMFILE="$(find "$TOPDIR/RPMS" -name '*.rpm' | head -1)"
[ -n "$RPMFILE" ] || { echo "error: rpmbuild produced no package" >&2; exit 1; }
cp "$RPMFILE" "$OUT/brauberg-sc880-${VERSION}-1.x86_64.rpm"
echo "built $OUT/brauberg-sc880-${VERSION}-1.x86_64.rpm"
