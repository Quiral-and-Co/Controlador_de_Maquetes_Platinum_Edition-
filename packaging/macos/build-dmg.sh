#!/usr/bin/env bash
set -euo pipefail
rid="${1:-osx-arm64}"
publish="${2:-artifacts/Platinum-$rid}"
app="artifacts/Platinum.app"
rm -rf "$app"
mkdir -p "$app/Contents/MacOS"
cp -a "$publish/." "$app/Contents/MacOS/"
cat > "$app/Contents/Info.plist" <<'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>Platinum</string>
<key>CFBundleDisplayName</key><string>Platinum</string>
<key>CFBundleIdentifier</key><string>com.quiralco.platinum</string>
<key>CFBundleVersion</key><string>0.1.0</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleExecutable</key><string>Platinum.Presentation.Avalonia</string>
</dict></plist>
EOF
chmod +x "$app/Contents/MacOS/Platinum.Presentation.Avalonia"
hdiutil create -volname Platinum -srcfolder "$app" -ov -format UDZO "artifacts/Platinum-Setup-$rid.dmg"
