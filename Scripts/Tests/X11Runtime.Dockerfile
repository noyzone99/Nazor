ARG LIBC=glibc
FROM ubuntu:22.04 AS glibc
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    xvfb openbox x11-utils xclip xdotool libxcursor1 libx11-6 libxext6 libxrandr2 libicu70 libssl3 libstdc++6 libfontconfig1 fonts-dejavu-core \
    libegl1-mesa libgles2-mesa libgl1-mesa-dri dbus \
    && rm -rf /var/lib/apt/lists/*
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends weston \
    && rm -rf /var/lib/apt/lists/*
FROM alpine:3.21 AS musl
RUN apk add --no-cache xvfb openbox xprop xclip xdotool libxcursor libx11 libxext libxrandr icu-libs libstdc++ fontconfig font-dejavu mesa-egl mesa-gles mesa-dri-gallium dbus
RUN apk add --no-cache weston weston-backend-headless
FROM ${LIBC} AS runtime
COPY Sources/Platforms/Nazor.Platform.Linux/Native/x11-test-server.sh /x11-test-server.sh
COPY Scripts/Tests/verify-x11-runtime.sh /verify-x11-runtime.sh
RUN sed -i 's/\r$//' /x11-test-server.sh /verify-x11-runtime.sh
ENTRYPOINT ["/bin/sh", "/verify-x11-runtime.sh"]
