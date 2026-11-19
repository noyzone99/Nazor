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
      -noLogo -noColor -class Nazor.Linux.Tests.X11Tests -class Nazor.Linux.Tests.DesktopLifecycleTests -class Nazor.Linux.Tests.PopupTests -result-xml "$output/x11.xml"
    ;;
  wayland|xwayland) ;;
  *) echo 'Use wayland, x11 or xwayland.' >&2; exit 2;;
esac
test_runtime=$(mktemp -d)
chmod 700 "$test_runtime"
export XDG_RUNTIME_DIR="$test_runtime" WAYLAND_DISPLAY=nazor-regression XDG_SESSION_TYPE=wayland NAZOR_TEST_COMPOSITOR=1 NAZOR_RENDERER=CPU
unset WAYLAND_SOCKET NAZOR_TEST_X11
if weston --help 2>&1 | grep -q -- '--renderer'; then renderer=--renderer=pixman; else renderer=--use-pixman; fi
if [ "$protocol" = xwayland ]; then
  export NAZOR_TEST_X11=1 NAZOR_LINUX_BACKEND=X11 NAZOR_TEST_EXIT_FILE="$test_runtime/exit-code"
  unset NAZOR_TEST_COMPOSITOR DISPLAY
  xwayland=${NAZOR_XWAYLAND:-$(command -v Xwayland)}
  printf '[xwayland]\npath=%s\n' "$xwayland" > "$test_runtime/weston.ini"
  trap 'rm -rf "$test_runtime"' EXIT
  weston --backend=headless-backend.so "$renderer" --xwayland --socket="$WAYLAND_DISPLAY" --idle-time=0 \
    --config="$test_runtime/weston.ini" --log="$output/xwayland-weston.log" \
    -- sh -c '"$@"; result=$?; printf "%s\n" "$result" > "$NAZOR_TEST_EXIT_FILE"' sh \
    "$dotnet_host" "$managed" -noLogo -noColor \
    -method '*DesktopLifecycleTests.Complete_decoration_combinations_match_capabilities_and_preserve_the_mapped_window' \
    -result-xml "$output/xwayland.xml"
  test -f "$NAZOR_TEST_EXIT_FILE" || { cat "$output/xwayland-weston.log"; exit 1; }
  exit "$(cat "$NAZOR_TEST_EXIT_FILE")"
fi
weston --backend=headless-backend.so "$renderer" --socket="$WAYLAND_DISPLAY" --idle-time=0 --no-config --log="$output/weston.log" &
compositor=$!
cleanup() { kill "$compositor" 2>/dev/null || true; wait "$compositor" 2>/dev/null || true; rm -rf "$test_runtime"; }
trap cleanup EXIT
for attempt in {1..100}; do test -S "$test_runtime/$WAYLAND_DISPLAY" && break; sleep .05; done
test -S "$test_runtime/$WAYLAND_DISPLAY" || { cat "$output/weston.log"; exit 1; }
dbus-run-session -- "$dotnet_host" "$managed" -noLogo -noColor -result-xml "$output/wayland.xml"
