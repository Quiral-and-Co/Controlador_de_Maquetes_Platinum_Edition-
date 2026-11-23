#!/usr/bin/env bash
set -euo pipefail
rid="${1:-linux-x64}"
publish="${2:-artifacts/Platinum-$rid}"
version="${3:-0.1.0}"
root="artifacts/deb-root"
rm -rf "$root"
mkdir -p "$root/DEBIAN" "$root/usr/lib/platinum" "$root/usr/bin" "$root/usr/share/applications"
cp -a "$publish/." "$root/usr/lib/platinum/"
cat > "$root/DEBIAN/control" <<EOF
Package: platinum-edition
Version: $version
Section: utils
Priority: optional
Architecture: amd64
Maintainer: Quiral-and-Co
Description: Platinum railway model control and layout editor
EOF
cat > "$root/usr/bin/platinum" <<'EOF'
#!/usr/bin/env sh
exec /usr/lib/platinum/Platinum.Presentation.Avalonia "$@"
EOF
chmod 0755 "$root/usr/bin/platinum" "$root/usr/lib/platinum/Platinum.Presentation.Avalonia"
cat > "$root/usr/share/applications/platinum.desktop" <<'EOF'
[Desktop Entry]
Name=Platinum
Comment=Railway model control and layout editor
Exec=platinum
Terminal=false
Type=Application
Categories=Office;Utility;
EOF
dpkg-deb --build --root-owner-group "$root" "artifacts/Platinum-Setup-$rid.deb"
