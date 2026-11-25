#!/bin/sh
set -eu
test_runtime=$(mktemp -d)
# A readiness probe can disconnect before Openbox has opened its display.
if [ "${NAZOR_X11_TCP_ONLY:-0}" = 1 ]; then
  Xvfb -noreset -screen 0 800x600x24 -nolisten unix -nolisten local -listen tcp -displayfd 3 3>"$test_runtime/display" >"$test_runtime/xvfb.log" 2>&1 &
else
  Xvfb -noreset -screen 0 800x600x24 -nolisten tcp -displayfd 3 3>"$test_runtime/display" >"$test_runtime/xvfb.log" 2>&1 &
fi
server=$!
manager=
compositor=
stop_process() {
  test_pid=$1
  kill "$test_pid" 2>/dev/null || true
  stop_attempt=0
  while kill -0 "$test_pid" 2>/dev/null && [ "$stop_attempt" -lt 40 ]; do stop_attempt=$((stop_attempt+1)); sleep .05; done
  kill -KILL "$test_pid" 2>/dev/null || true
  wait "$test_pid" 2>/dev/null || true
}
cleanup() { if [ -n "$compositor" ]; then stop_process "$compositor"; fi; if [ -n "$manager" ]; then stop_process "$manager"; fi; stop_process "$server"; rm -rf "$test_runtime"; }
trap cleanup EXIT
attempt=0
while [ ! -s "$test_runtime/display" ]; do
  attempt=$((attempt+1)); if [ "$attempt" -gt 200 ] || ! kill -0 "$server" 2>/dev/null; then
    echo 'Xvfb did not report a display.' >&2
    cat "$test_runtime/xvfb.log"; exit 1
  fi
  sleep .05
done
export DISPLAY=:$(cat "$test_runtime/display")
if [ "${NAZOR_X11_TCP_ONLY:-0}" = 1 ]; then DISPLAY=127.0.0.1"$DISPLAY"; export DISPLAY; fi
unset WAYLAND_DISPLAY WAYLAND_SOCKET
openbox --sm-disable >"$test_runtime/openbox.log" 2>&1 &
manager=$!
attempt=0
while :; do
  case $(xprop -root _NET_CLIENT_LIST 2>/dev/null || true) in
    '_NET_CLIENT_LIST(WINDOW):'*) break ;;
  esac
  attempt=$((attempt+1)); if [ "$attempt" -gt 200 ] || ! kill -0 "$manager" 2>/dev/null; then
    echo "Openbox did not register a window manager on DISPLAY=$DISPLAY." >&2
    cat "$test_runtime/openbox.log" "$test_runtime/xvfb.log"; exit 1
  fi
  sleep .05
done
if [ "${NAZOR_TEST_X11_COMPOSITING:-0}" = 1 ]; then
  xcompmgr >"$test_runtime/xcompmgr.log" 2>&1 &
  compositor=$!
  sleep .2
  kill -0 "$compositor" 2>/dev/null || { cat "$test_runtime/xcompmgr.log"; exit 1; }
fi
if "$@"; then exit 0; else
  result=$?
  cat "$test_runtime/openbox.log" "$test_runtime/xvfb.log"
  xprop -root _NET_CLIENT_LIST _NET_ACTIVE_WINDOW || true
  xwininfo -root -tree 2>/dev/null || true
  if ! kill -0 "$manager" 2>/dev/null; then echo 'The X11 window manager exited.' >&2; fi
  exit "$result"
fi
