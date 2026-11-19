#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
configuration=${1:-Release}
[[ $# = 0 ]] || shift
case "$configuration" in Debug|Release) ;; *) echo 'Use Debug or Release.' >&2; exit 2;; esac
rids=("$@")
[[ ${#rids[@]} != 0 ]] || rids=(linux-x64 linux-arm64 linux-musl-x64 linux-musl-arm64)
for rid in "${rids[@]}"; do
  case "$rid" in
    linux-x64) platform=linux/amd64; libc=glibc;;
    linux-arm64) platform=linux/arm64; libc=glibc;;
    linux-musl-x64) platform=linux/amd64; libc=musl;;
    linux-musl-arm64) platform=linux/arm64; libc=musl;;
    *) echo "Unsupported native RID: $rid" >&2; exit 2;;
  esac
  mkdir -p "$root/artifacts/native-build/$rid/$configuration"
  docker buildx build --platform "$platform" --build-arg "LIBC=$libc" --build-arg "CONFIGURATION=$configuration" \
    --file "$root/Scripts/Build/Native.Dockerfile" --target artifact \
    --metadata-file "$root/artifacts/native-build/$rid/$configuration/build-metadata.json" \
    --output "type=local,dest=$root/artifacts/native" "$root/Sources/Platforms/Nazor.Platform.Linux/Native"
  test -s "$root/artifacts/native/$rid/libnazor-wayland.so"
  test -s "$root/artifacts/native/$rid/libnazor-x11.so"
  echo "Built $rid: $root/artifacts/native/$rid/libnazor-wayland.so"
done
