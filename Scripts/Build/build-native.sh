#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
configuration=${1:-Release}
expected_rid=${2:-}
backend=${3:-All}
case "$backend" in All) wayland=ON; x11=ON;; Wayland) wayland=ON; x11=OFF;; X11) wayland=OFF; x11=ON;; *) echo 'Use All, Wayland or X11.' >&2; exit 2;; esac
case "$configuration" in Debug|Release) ;; *) echo 'Use Debug or Release.' >&2; exit 2;; esac
read -r -a compiler <<< "${CC:-cc}"
target=$("${compiler[@]}" -dumpmachine)
case "$target" in
  x86_64-*) arch=x64;;
  aarch64-*) arch=arm64;;
  *) echo "Unsupported compiler target: $target. Use x64 or ARM64." >&2; exit 2;;
esac
definitions=$(printf '#include <features.h>\n' | "${compiler[@]}" -dM -E -)
if [[ "$definitions" == *'define __GLIBC__ '* ]]; then
  rid="linux-$arch"
elif [[ "$target" == *musl* || "$(ldd --version 2>&1 || true)" == *musl* ]]; then
  rid="linux-musl-$arch"
else
  echo 'Cannot identify the compiler libc; refusing to label the native library.' >&2; exit 2
fi
if [[ -n "$expected_rid" && "$expected_rid" != "$rid" ]]; then
  echo "The compiler targets $rid, not $expected_rid." >&2; exit 2
fi
build=${NAZOR_NATIVE_BUILD:-"$root/artifacts/native-build/$rid/$configuration"}
cmake -S "$root/Sources/Platforms/Nazor.Platform.Linux/Native" -B "$build" \
  -DCMAKE_BUILD_TYPE="$configuration" -DCMAKE_LIBRARY_OUTPUT_DIRECTORY="$root/artifacts/native/$rid" -DBUILD_TESTING=ON -DNAZOR_WAYLAND="$wayland" -DNAZOR_X11="$x11"
cmake --build "$build" --parallel 2
env -u WAYLAND_SOCKET WAYLAND_DISPLAY=nazor-missing-display ctest --test-dir "$build" --output-on-failure \
  | tee "$root/artifacts/native/$rid/native-tests.txt"
{ "${compiler[@]}" --version; \
  for module in wayland x11; do \
    if [[ "$backend" == Wayland && "$module" == x11 || "$backend" == X11 && "$module" == wayland ]]; then continue; fi; \
    library="$root/artifacts/native/$rid/libnazor-$module.so"; \
    readelf -h "$library"; readelf -d "$library"; readelf --version-info "$library"; sha256sum "$library"; \
  done; \
} > "$root/artifacts/native/$rid/native-build.txt"
