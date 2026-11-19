#!/bin/sh
set -eu
ulimit -c 0
weston=$1
xwayland=$2
test_binary=$3
shift 3
if [ "$#" = 0 ]; then set -- --outer-resize; fi
if ! "$weston" --help 2>&1 | grep -q -- '-- program'; then
  echo 'SKIP: the nested input test requires Weston with client launch support.'
  exit 77
fi
test_runtime=$(mktemp -d)
server=
cleanup() { if [ -n "$server" ]; then kill "$server" 2>/dev/null || true; wait "$server" 2>/dev/null || true; fi; rm -rf "$test_runtime"; }
trap cleanup EXIT
chmod 700 "$test_runtime"
Xvfb -screen 0 1000x800x24 -nolisten tcp -displayfd 3 3>"$test_runtime/display" >"$test_runtime/xvfb.log" 2>&1 &
server=$!
attempt=0
while [ ! -s "$test_runtime/display" ]; do
  attempt=$((attempt+1)); if [ "$attempt" -gt 100 ] || ! kill -0 "$server" 2>/dev/null; then cat "$test_runtime/xvfb.log"; exit 1; fi
  sleep .05
done
export DISPLAY=:$(cat "$test_runtime/display") NAZOR_TEST_OUTER_DISPLAY=:$(cat "$test_runtime/display")
export XDG_RUNTIME_DIR="$test_runtime" NAZOR_TEST_EXIT_FILE="$test_runtime/result"
unset WAYLAND_DISPLAY WAYLAND_SOCKET
printf '[xwayland]\npath=%s\n' "$xwayland" > "$test_runtime/weston.ini"
"$weston" --backend=x11 --renderer=pixman --width=800 --height=600 --xwayland --idle-time=0 \
  --config="$test_runtime/weston.ini" --socket=nazor-outer-resize --log="$test_runtime/weston.log" \
  -- sh -c '"$@"; result=$?; printf "%s\n" "$result" > "$NAZOR_TEST_EXIT_FILE"' sh "$test_binary" "$@" || {
    cat "$test_runtime/weston.log"; exit 1;
  }
if [ ! -f "$NAZOR_TEST_EXIT_FILE" ]; then cat "$test_runtime/weston.log"; exit 1; fi
result=$(cat "$NAZOR_TEST_EXIT_FILE")
if [ "$result" != 0 ]; then cat "$test_runtime/weston.log"; fi
exit "$result"
