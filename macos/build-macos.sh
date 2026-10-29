#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
rid="${1:-}"
case "$rid" in
 osx-arm64) expected_arch=arm64 ;;
 osx-x64) expected_arch=x86_64 ;;
 *) echo 'Usage: bash macos/build-macos.sh osx-arm64|osx-x64 (on a matching Mac)' >&2; exit 2 ;;
esac
if [[ "$(uname -s)" != Darwin || "$(uname -m)" != "$expected_arch" ]]; then
 echo "Build and smoke checks must run on a native $expected_arch Mac." >&2;exit 2
fi
output="macos/dist/$rid"
staging="$output/image"
bundle="$staging/Gamma CNF Reader.app"
mkdir -p "$bundle/Contents/MacOS" "$bundle/Contents/Resources"
dotnet run --project windows/GammaCnf.Checks -c Release
dotnet publish macos/GammaCnf.Desktop -c Release -r "$rid" --self-contained true \
 -p:PublishSingleFile=false -p:PublishTrimmed=false -o "$bundle/Contents/MacOS"
cp macos/packaging/Info.plist "$bundle/Contents/Info.plist"
cp macos/README.md "$staging/Read Me - Huong dan.md"
cp DISTRIBUTION-LICENSE.txt "$staging/"
cp macos/THIRD-PARTY-NOTICES.md "$bundle/Contents/Resources/"
python3 macos/packaging/licenses.py macos/GammaCnf.Desktop/obj/project.assets.json "$bundle/Contents/Resources/Licenses"
chmod +x "$bundle/Contents/MacOS/GammaCNFReader"
/usr/libexec/PlistBuddy -c 'Print CFBundleExecutable' "$bundle/Contents/Info.plist"
file "$bundle/Contents/MacOS/GammaCNFReader" | tee "$output/architecture.txt"
lipo -verify_arch "$expected_arch" "$bundle/Contents/MacOS/GammaCNFReader"
# Local ad-hoc integrity signature; this is NOT an Apple Developer ID/notarization.
codesign --force --deep --sign - --entitlements macos/packaging/entitlements.plist "$bundle"
codesign --verify --deep --strict --verbose=2 "$bundle"
ln -s /Applications "$staging/Applications"
dmg="$output/GammaCNFReader-macOS-$rid.dmg"
hdiutil create -volname "Gamma CNF Reader $rid" -srcfolder "$staging" -ov -format UDZO "$dmg"
hdiutil verify "$dmg"
# Test the actual application from the mounted distributable, not a build output.
mount="$output/mounted"
mkdir -p "$mount"
hdiutil attach "$dmg" -readonly -nobrowse -mountpoint "$mount"
trap 'hdiutil detach "$mount" || true' EXIT
fixture="${RUNNER_TEMP:-/tmp}/gamma-$rid-fixture.cnf"
evidence="${RUNNER_TEMP:-/tmp}/gamma-$rid-evidence"
dotnet run --project windows/GammaCnf.Checks -c Release -- --write-fixture "$fixture"
mkdir -p "$evidence"
# A subprocess timeout catches native crashes and UI hangs and reports smoke errors.
python3 macos/packaging/smoke.py "$mount/Gamma CNF Reader.app/Contents/MacOS/GammaCNFReader" "$fixture" "$evidence"
codesign --verify --deep --strict "$mount/Gamma CNF Reader.app"
