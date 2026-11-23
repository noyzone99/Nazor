ARG LIBC=glibc
FROM ubuntu:22.04 AS glibc
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    build-essential cmake ninja-build pkg-config libwayland-dev libwayland-bin libxkbcommon-dev binutils \
    && rm -rf /var/lib/apt/lists/*
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends xkb-data libx11-data \
    && rm -rf /var/lib/apt/lists/*
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends libx11-dev libxcursor-dev libxrender-dev libxext-dev libxrandr-dev \
    && rm -rf /var/lib/apt/lists/*

FROM alpine:3.21 AS musl
RUN apk add --no-cache build-base cmake ninja pkgconf wayland-dev libxkbcommon-dev binutils
RUN apk add --no-cache xkeyboard-config libx11
RUN apk add --no-cache libx11-dev libxcursor-dev libxrender-dev libxext-dev libxrandr-dev

FROM ${LIBC} AS build
ARG LIBC
ARG TARGETARCH
ARG CONFIGURATION=Release
COPY . /source
RUN case "$TARGETARCH" in amd64) arch=x64;; arm64) arch=arm64;; *) exit 2;; esac \
    && case "$LIBC" in glibc) rid=linux-$arch;; musl) rid=linux-musl-$arch;; *) exit 2;; esac \
    && cmake -S /source -B /build -G Ninja -DCMAKE_BUILD_TYPE="$CONFIGURATION" -DBUILD_TESTING=ON -DNAZOR_X11_WINDOW_TESTS=OFF \
    && cmake --build /build --parallel 2 \
    && case "$TARGETARCH" in amd64) machine=X86-64;; arm64) machine=AArch64;; esac \
    && readelf -h /build/libnazor-wayland.so | grep -q "Machine:.*$machine" \
    && readelf -d /build/libnazor-wayland.so | grep -q 'SONAME.*\[libnazor-wayland.so\]' \
    && readelf -h /build/libnazor-x11.so | grep -q "Machine:.*$machine" \
    && readelf -d /build/libnazor-x11.so | grep -q 'SONAME.*\[libnazor-x11.so\]' \
    && ! readelf -d /build/libnazor-x11.so | grep -E 'NEEDED.*(wayland|gtk|Qt)' \
    && ! readelf -d /build/libnazor-wayland.so | grep -E 'NEEDED.*(libX11|libXcursor)' \
    && case "$LIBC" in \
         glibc) readelf -d /build/libnazor-wayland.so | grep -q 'NEEDED.*\[libc.so.6\]'; readelf -d /build/libnazor-x11.so | grep -q 'NEEDED.*\[libc.so.6\]';; \
         musl) readelf -d /build/libnazor-wayland.so | grep -q 'NEEDED.*\[libc.musl-'; readelf -d /build/libnazor-x11.so | grep -q 'NEEDED.*\[libc.musl-';; \
       esac \
    && mkdir -p "/output/$rid" \
    && { WAYLAND_DISPLAY=nazor-missing-display ctest --test-dir /build --output-on-failure \
       > "/output/$rid/native-tests.txt" 2>&1 || { cat "/output/$rid/native-tests.txt"; exit 1; }; } \
    && cp /build/libnazor-wayland.so /build/libnazor-x11.so "/output/$rid/" \
    && mkdir -p "/output/$rid/tests" && cp /build/nazor-x11-tests "/output/$rid/tests/" \
    && { cc --version; pkg-config --modversion wayland-client wayland-cursor xkbcommon; \
         readelf -h /build/libnazor-wayland.so; readelf -d /build/libnazor-wayland.so; readelf --version-info /build/libnazor-wayland.so; \
         sha256sum /build/libnazor-wayland.so; \
         readelf -h /build/libnazor-x11.so; readelf -d /build/libnazor-x11.so; readelf --version-info /build/libnazor-x11.so; \
         sha256sum /build/libnazor-x11.so; } > "/output/$rid/native-build.txt"

FROM scratch AS artifact
COPY --from=build /output/ /
