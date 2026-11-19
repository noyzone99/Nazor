#!/bin/sh
set -eu
export NAZOR_RENDERER=CPU NAZOR_TEST_X11=1
if [ "$#" -eq 0 ]; then set -- /app/Deployment /out/frame.bgra; fi
exec sh /x11-test-server.sh "$@"
