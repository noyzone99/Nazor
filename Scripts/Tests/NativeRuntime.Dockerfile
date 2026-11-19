ARG LIBC=glibc
FROM ubuntu:22.04 AS glibc
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    weston libicu70 libssl3 libstdc++6 libfontconfig1 fonts-dejavu-core libxkbcommon0 libwayland-client0 libwayland-cursor0 \
    && rm -rf /var/lib/apt/lists/*

FROM alpine:3.21 AS musl
RUN apk add --no-cache weston weston-backend-headless icu-libs libstdc++ fontconfig font-dejavu libxkbcommon wayland-libs-client wayland-libs-cursor

FROM ${LIBC} AS runtime
COPY verify-native-runtime.sh /verify-native-runtime.sh
RUN sed -i 's/\r$//' /verify-native-runtime.sh
ENTRYPOINT ["/bin/sh", "/verify-native-runtime.sh"]
