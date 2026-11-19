#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "$0")/../.." && pwd)
workspace=${1:?Choose a fresh absolute consumer workspace}
framework=${2:-net10.0}
feed=${3:-$root/artifacts/packages}
mode=${4:-development}
[[ "$workspace" = /* && ! -e "$workspace" ]] || { echo 'Choose a fresh absolute consumer workspace.' >&2; exit 2; }
[[ "$framework" = net8.0 || "$framework" = net9.0 || "$framework" = net10.0 || "$framework" = net11.0 ]] || exit 2
[[ "$mode" = development || "$mode" = production ]] || exit 2
mkdir -p "$workspace/Probe"
version=$(sed -n 's/.*<NazorVersion>\(.*\)<\/NazorVersion>.*/\1/p' "$root/Directory.Build.props")
for name in Directory.Build.props Directory.Build.targets Directory.Packages.props; do echo '<Project />' > "$workspace/$name"; done
enabled=true
[[ "$mode" = production ]] && enabled=false
cat > "$workspace/Probe/Probe.csproj" <<EOF
<Project Sdk="Nazor.Sdk/$version"><PropertyGroup><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><TargetFramework>$framework</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><RootNamespace>DevToolsLaunch</RootNamespace><NazorEnableDevTools>$enabled</NazorEnableDevTools></PropertyGroup></Project>
EOF
cat > "$workspace/nuget.config" <<EOF
<configuration><config><add key="globalPackagesFolder" value="$workspace/.packages" /></config><packageSources><clear /><add key="nazor" value="$feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources><packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping><fallbackPackageFolders><clear /></fallbackPackageFolders></configuration>
EOF
cp "$root/Tests/Fixtures/DevToolsLaunch/Program.cs" "$workspace/Probe/Program.cs"
dotnet_command=(dotnet)
[[ -n "${NAZOR_TEST_SDK_DRIVER:-}" ]] && dotnet_command+=("$NAZOR_TEST_SDK_DRIVER")
arguments=(publish "$workspace/Probe/Probe.csproj" -c Release -o "$workspace/output" -m:1 -nodeReuse:false -v:q)
[[ "$mode" = production ]] && arguments+=(-p:PublishProfile=NazorProduction)
[[ "$mode" = development ]] && arguments+=(-p:NazorEnablePublishValidation=false)
cd "$workspace"
"${dotnet_command[@]}" "${arguments[@]}" > "$workspace/build.log" 2>&1
"${NAZOR_TEST_NODE:-node}" "$root/Scripts/Tests/verify-devtools-launch.mjs" "$workspace/output/Probe" "$workspace/runtime/gate" "$mode"
