#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
configuration=${1:-Debug}
case "$configuration" in Debug|Release) ;; *) echo 'Use Debug or Release.' >&2; exit 2;; esac
build=${NAZOR_LINUX_ARTIFACTS:-"$root/artifacts/wayland-tests"}
mkdir -p "$build"
bash "$root/Scripts/Build/build-native.sh" "$configuration"
dotnet build "$root/Nazor.slnx" -c "$configuration" --artifacts-path "$build/managed" -m:1 -p:UseSharedCompilation=false
flavor=$(printf '%s' "$configuration" | tr '[:upper:]' '[:lower:]')
for suite in Nazor.Core.Tests Nazor.Style.Tests Nazor.Layout.Tests Nazor.Compiler.Tests Nazor.Tooling.Tests Nazor.Controls.Tests Nazor.Rendering.Tests; do
  dotnet "$build/managed/bin/$suite/$flavor/$suite.dll" \
    -noColor -noLogo -reporter quiet -result-xml "$build/$suite.xml"
done
for framework in net8.0 net9.0 net10.0; do
  NAZOR_DEVTOOLS_FIXTURE="$build/managed/bin/DevToolsAgent/${flavor}_$framework/DevToolsAgent.dll" \
    dotnet "$build/managed/bin/Nazor.DevTools.Tests/$flavor/Nazor.DevTools.Tests.dll" \
      -noColor -noLogo -reporter quiet -result-xml "$build/shell-$framework.xml"
  NAZOR_DEVTOOLS_FIXTURE="$build/managed/bin/DevToolsAgent/${flavor}_$framework/DevToolsAgent.dll" \
    dotnet "$build/managed/bin/Nazor.DevTools.Agent.Tests/$flavor/Nazor.DevTools.Agent.Tests.dll" \
      -noColor -noLogo -reporter quiet -result-xml "$build/agent-$framework.xml"
  dotnet "$build/managed/bin/Nazor.DevTools.Protocol.Tests/${flavor}_$framework/Nazor.DevTools.Protocol.Tests.dll" \
    -noColor -noLogo -reporter quiet -result-xml "$build/protocol-$framework.xml"
done

dotnet "$build/managed/bin/Nazor.Samples.Counter/$flavor/Nazor.Samples.Counter.dll" --headless --png "$build/counter"
runtime=$(mktemp -d /tmp/nazor-wayland-test.XXXXXX)
chmod 700 "$runtime"
export XDG_RUNTIME_DIR="$runtime" WAYLAND_DISPLAY=nazor-test NAZOR_TEST_COMPOSITOR=1
unset WAYLAND_SOCKET
weston --backend=headless --renderer=pixman --socket="$WAYLAND_DISPLAY" --idle-time=0 --no-config --log="$build/weston.log" &
compositor=$!
cleanup() { kill "$compositor" 2>/dev/null || true; wait "$compositor" 2>/dev/null || true; }
trap cleanup EXIT
for attempt in {1..100}; do
  test -S "$runtime/$WAYLAND_DISPLAY" && break
  kill -0 "$compositor" 2>/dev/null || { cat "$build/weston.log"; exit 1; }
  sleep 0.05
done
test -S "$runtime/$WAYLAND_DISPLAY" || { cat "$build/weston.log"; exit 1; }
native_build=${NAZOR_NATIVE_BUILD:-"$root/artifacts/native-build/$(dotnet msbuild "$root/Sources/Platforms/Nazor.Platform.Linux/Nazor.Platform.Linux.csproj" -nologo -getProperty:NazorNativeRid)/$configuration"}
"$native_build/nazor-wayland-tests"
dbus-run-session -- dotnet "$build/managed/bin/Nazor.Linux.Tests/$flavor/Nazor.Linux.Tests.dll" \
  -noColor -noLogo -result-xml "$build/linux-tests.xml"
if ldd "$root/artifacts/native/linux-x64/libnazor-wayland.so" | grep -E 'libgtk|libQt'; then
  echo 'The Wayland bridge must not depend on GTK or Qt.' >&2
  exit 1
fi
