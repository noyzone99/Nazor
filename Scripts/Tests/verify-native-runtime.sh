#!/bin/sh
set -eu
export XDG_RUNTIME_DIR=/tmp/nazor-runtime WAYLAND_DISPLAY=nazor-runtime NAZOR_RENDERER=CPU
unset WAYLAND_SOCKET
mkdir -p "$XDG_RUNTIME_DIR"
chmod 700 "$XDG_RUNTIME_DIR"
if weston --help 2>&1 | grep -q -- '--renderer'; then
  renderer=--renderer=pixman
else
  renderer=--use-pixman
fi
weston --backend=headless-backend.so "$renderer" --shell=kiosk-shell.so --width=320 --height=200 --socket="$WAYLAND_DISPLAY" --idle-time=0 --no-config --log=/out/weston.log &
compositor=$!
trap 'kill "$compositor" 2>/dev/null || true; wait "$compositor" 2>/dev/null || true' EXIT
attempt=0
while [ ! -S "$XDG_RUNTIME_DIR/$WAYLAND_DISPLAY" ]; do
  attempt=$((attempt+1))
  if [ "$attempt" -gt 100 ] || ! kill -0 "$compositor" 2>/dev/null; then cat /out/weston.log; exit 1; fi
  sleep .1
done
cd /app
if timeout 30s ./Deployment /out/frame.bgra > /out/runtime.log 2>&1; then
  cat /out/runtime.log
else
  cat /out/runtime.log
  exit 1
fi
grep -q 'PASS: native window' /out/runtime.log
test -s /out/frame.bgra
