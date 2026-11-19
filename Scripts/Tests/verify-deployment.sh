#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "$0")/../.." && pwd)
mode=${1:-single}
framework=${3:-net10.0}
backend=${NAZOR_TEST_RENDERER:-CPU}
extraction=${NAZOR_TEST_SINGLE_FILE_EXTRACTION:-Default}
[[ "$extraction" = Default || "$extraction" = All ]]
[[ "$mode" = single || "$mode" = trim || "$mode" = trim-single || "$mode" = aot ]]
output=${2:-"$root/artifacts/deployment/linux"}
mkdir -p "$output"
output=$(cd "$output" && pwd)
project=${NAZOR_TEST_PROJECT:-"$root/Tests/Fixtures/Deployment/Deployment.csproj"}
dotnet_command=(dotnet msbuild)
[[ -n "${NAZOR_TEST_SDK_DRIVER:-}" ]] && dotnet_command=(dotnet "$NAZOR_TEST_SDK_DRIVER")
for variant in directory "$mode"; do
    options=(-p:PublishAot=false -p:PublishSingleFile=false -p:PublishTrimmed=false)
    [[ "$variant" = trim ]] && options=(-p:PublishAot=false -p:PublishSingleFile=false -p:PublishTrimmed=true)
    if [[ "$variant" = single || "$variant" = trim-single ]]; then
        options=(-p:PublishAot=false -p:PublishSingleFile=true -p:PublishTrimmed=false)
        [[ "$variant" = trim-single ]] && options=(-p:PublishAot=false -p:PublishSingleFile=true -p:PublishTrimmed=true)
        [[ "$extraction" = All ]] && options+=(-p:IncludeAllContentForSelfExtract=true -p:IncludeNativeLibrariesForSelfExtract=true)
    fi
    [[ "$variant" = aot ]] && options=(-p:PublishAot=true -p:PublishSingleFile=false)
    if [[ "${NAZOR_TEST_SKIP_PUBLISH:-0}" != 1 ]]; then
        "${dotnet_command[@]}" "$project" -restore -t:Publish -p:Configuration=Release -p:NazorDeploymentFramework="$framework" -p:RuntimeIdentifier=linux-x64 -p:SelfContained=true -p:NazorEnablePublishValidation=true -p:NazorEnableDevTools=false -p:NuGetAudit=false -p:UseArtifactsOutput=true -p:ArtifactsPath="${NAZOR_TEST_BUILD_DIR:-/tmp/nazor-deployment-build}" -p:PublishDir="$output/$variant/" -m:1 "${options[@]}" > "$output/$variant-build.log" 2>&1
    fi
    directory="$output/$variant"
    if [[ ( "$variant" = single || "$variant" = trim-single ) && "$extraction" = All ]]; then
        directory="$output/isolated"
        mkdir -p "$directory"
        [[ -z "$(find "$directory" -mindepth 1 ! -name Deployment -print -quit)" ]]
        cp "$output/$variant/Deployment" "$directory/Deployment"
        chmod +x "$directory/Deployment"
    fi
    (cd "$directory" && NAZOR_RENDERER="$backend" timeout 30s ./Deployment "$output/$variant-$backend.bgra") > "$output/$variant-$backend-run.log" 2>&1
    grep -q 'PASS: native window' "$output/$variant-$backend-run.log"
done
cmp "$output/directory-$backend.bgra" "$output/$mode-$backend.bgra"
if [[ "$mode" = aot || "$mode" = trim || "$mode" = trim-single ]]; then
    if [[ "${NAZOR_TEST_SKIP_PUBLISH:-0}" != 1 ]]; then
        "${dotnet_command[@]}" "$root/Tests/Fixtures/DBusAot/DBusAot.csproj" -restore -t:Publish -p:Configuration=Release -p:NazorDeploymentFramework="$framework" -p:RuntimeIdentifier=linux-x64 -p:SelfContained=true -p:NazorEnablePublishValidation=true -p:NuGetAudit=false -p:UseArtifactsOutput=true -p:ArtifactsPath="${NAZOR_TEST_BUILD_DIR:-/tmp/nazor-deployment-build}" -p:PublishDir="$output/dbus-$mode/" -m:1 "${options[@]}" > "$output/dbus-$mode-build.log" 2>&1
    fi
    dbus-run-session -- timeout 30s "$output/dbus-$mode/DBusAot" > "$output/dbus-$mode-run.log" 2>&1
    grep -q 'PASS: D-Bus' "$output/dbus-$mode-run.log"
fi
echo "PASS: Linux $mode $backend native behavior and identical pixels. Evidence: $output"
