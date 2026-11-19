FROM ubuntu:22.04
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    clang build-essential zlib1g-dev libicu70 libssl3 ca-certificates \
    && rm -rf /var/lib/apt/lists/*
ENV DOTNET_ROOT=/dotnet
ENV PATH=/dotnet:$PATH
