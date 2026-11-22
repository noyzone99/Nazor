#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
configuration=${1:-Release}
case "$configuration" in Debug|Release) ;; *) echo 'Use Debug or Release.' >&2; exit 2;; esac
build=${NAZOR_DRM_ARTIFACTS:-"$root/artifacts/drm-tests"}
mkdir -p "$build"
managed=${2:-}
if [[ -z "$managed" ]]; then
  dotnet build "$root/Tests/Nazor.Linux.Tests" -c "$configuration" --artifacts-path "$build/managed" -m:1 -p:UseSharedCompilation=false
  flavor=$(printf '%s' "$configuration" | tr '[:upper:]' '[:lower:]')
  managed="$build/managed/bin/Nazor.Linux.Tests/$flavor/Nazor.Linux.Tests.dll"
fi
test -f "$managed" || { echo 'Pass an existing Nazor.Linux.Tests.dll as the second argument.' >&2; exit 2; }
gcc -shared -fPIC -std=c11 -Wall -Wextra -Werror -pthread \
  "$root/Tests/Nazor.Linux.Tests/Native/drm-fixture.c" -o "$build/libnazor-drm-fixture.so"
NAZOR_DRM_TEST_LIBRARY="$build/libnazor-drm-fixture.so" NAZOR_RENDERER=AUTO XKB_DEFAULT_LAYOUT=us \
  dotnet "$managed" -noLogo -noColor -class '*Drm*' -result-xml "$build/drm-tests.xml"
NAZOR_DRM_TEST_LIBRARY="$build/libnazor-drm-fixture.so" NAZOR_DRM_TEST_INPUT_FIXTURE=1 XKB_DEFAULT_LAYOUT=us \
  dotnet "$managed" -noLogo -noColor -method '*Input_callbacks_remain_valid_until_native_context_destruction' -result-xml "$build/drm-input-tests.xml"
