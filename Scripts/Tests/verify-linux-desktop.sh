#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
protocol=${1:?Use wayland, x11 or xwayland}
managed=${2:?Pass Nazor.Linux.Tests.dll}
dotnet_host=${NAZOR_DOTNET:-dotnet}
output=${NAZOR_DESKTOP_TEST_OUTPUT:-"$root/artifacts/linux-desktop-tests"}
mkdir -p "$output"
case "$protocol" in
  x11)
    export NAZOR_TEST_X11=1 NAZOR_RENDERER=CPU
    exec sh "$root/Sources/Platforms/Nazor.Platform.Linux/Native/x11-test-server.sh" "$dotnet_host" "$managed" \
      -noLogo -noColor -class Nazor.Linux.Tests.X11Tests -class Nazor.Linux.Tests.DesktopLifecycleTests \
      -class Nazor.Linux.Tests.PopupTests -class Nazor.Linux.Tests.GpuApplicationTests -result-xml "$output/x11.xml"
    ;;
  wayland|xwayland) ;;
  *) echo 'Use wayland, x11 or xwayland.' >&2; exit 2;;
esac
test_runtime=$(mktemp -d)
chmod 700 "$test_runtime"
export XDG_RUNTIME_DIR="$test_runtime" WAYLAND_DISPLAY=nazor-regression XDG_SESSION_TYPE=wayland NAZOR_TEST_COMPOSITOR=1 NAZOR_RENDERER=CPU
unset WAYLAND_SOCKET NAZOR_TEST_X11
if weston --help 2>&1 | grep -q -- '--renderer'; then renderer=--renderer=pixman; else renderer=--use-pixman; fi
compositor=
cleanup() {
  if [ -n "$compositor" ]; then kill "$compositor" 2>/dev/null || true; wait "$compositor" 2>/dev/null || true; fi
  rm -rf "$test_runtime"
}
trap cleanup EXIT
if [ "$protocol" = xwayland ]; then
  export NAZOR_TEST_X11=1 NAZOR_LINUX_BACKEND=X11
  unset NAZOR_TEST_COMPOSITOR DISPLAY
  xwayland=${NAZOR_XWAYLAND:-$(command -v Xwayland)}
  printf '[xwayland]\npath=%s\n' "$xwayland" > "$test_runtime/weston.ini"
  log="$output/xwayland-weston.log"
  : > "$log"
  weston --backend=headless-backend.so "$renderer" --xwayland --socket="$WAYLAND_DISPLAY" --idle-time=0 \
    --config="$test_runtime/weston.ini" >"$log" 2>&1 &
  compositor=$!
  for attempt in {1..100}; do
    DISPLAY=$(sed -nE 's/.*xserver listening on display (:[0-9]+).*/\1/p' "$log")
    [ -n "$DISPLAY" ] && break
    kill -0 "$compositor" 2>/dev/null || break
    sleep .05
  done
  test -n "$DISPLAY" || { cat "$log"; exit 1; }
  export DISPLAY
  "$dotnet_host" "$managed" -noLogo -noColor \
    -method '*DesktopLifecycleTests.Complete_decoration_combinations_match_capabilities_and_preserve_the_mapped_window' \
    -result-xml "$output/xwayland.xml"
  exit
fi
weston --backend=headless-backend.so "$renderer" --socket="$WAYLAND_DISPLAY" --idle-time=0 --no-config --log="$output/weston.log" &
compositor=$!
for attempt in {1..100}; do test -S "$test_runtime/$WAYLAND_DISPLAY" && break; sleep .05; done
test -S "$test_runtime/$WAYLAND_DISPLAY" || { cat "$output/weston.log"; exit 1; }
dbus-run-session -- "$dotnet_host" "$managed" -noLogo -noColor -result-xml "$output/wayland.xml"
