#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "$0")/../.." && pwd)
workspace=$1
feed=$(cd "$2" && pwd)
framework=${3:-net10.0}
[[ ! -e "$workspace" ]]
mkdir -p "$workspace/project" "$workspace/dbus"
workspace=$(cd "$workspace" && pwd)
for file in Directory.Build.props Directory.Build.targets Directory.Packages.props; do echo '<Project />' > "$workspace/$file"; done
version=$(dotnet msbuild "$root/Directory.Build.props" -nologo -getProperty:NazorVersion)
cat > "$workspace/project/Deployment.csproj" <<'PROJECT'
<Project Sdk="Nazor.Sdk/VERSION">
  <PropertyGroup><TargetFramework>$(NazorDeploymentFramework)</TargetFramework><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.deployment</NazorApplicationId><RootNamespace>Deployment</RootNamespace><AssemblyName>Deployment</AssemblyName><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
  <ItemGroup><None Update="payload.txt" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" /></ItemGroup>
  <ItemGroup><PackageReference Include="Nazor.Themes.NazorLeaf" Version="VERSION" /></ItemGroup>
</Project>
PROJECT
cat > "$workspace/dbus/DBusAot.csproj" <<'PROJECT'
<Project Sdk="Nazor.Sdk/VERSION">
  <PropertyGroup><TargetFramework>$(NazorDeploymentFramework)</TargetFramework><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><NazorImplicitRuntimeReference>false</NazorImplicitRuntimeReference><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
  <ItemGroup><PackageReference Include="Nazor.Platform.Linux" Version="VERSION" /></ItemGroup>
</Project>
PROJECT
sed -i "s/VERSION/$version/g" "$workspace/project/Deployment.csproj" "$workspace/dbus/DBusAot.csproj"
cp "$root/Tests/Fixtures/Deployment/Program.cs" "$root/Tests/Fixtures/Deployment/Probe.nazor" "$root/Tests/Fixtures/Deployment/payload.txt" "$workspace/project/"
cp "$root/Tests/Fixtures/DBusAot/Program.cs" "$workspace/dbus/"
cat > "$workspace/nuget.config" <<'CONFIG'
<configuration>
  <config><add key="globalPackagesFolder" value=".packages" /></config>
  <packageSources><clear /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
CONFIG
dotnet nuget add source "$feed" -n nazor --configfile "$workspace/nuget.config" > "$workspace/sources.log"
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org --configfile "$workspace/nuget.config" >> "$workspace/sources.log"
export NAZOR_TEST_PROJECT="$workspace/project/Deployment.csproj"
for mode in ${NAZOR_TEST_DEPLOYMENT_MODES:-single trim trim-single aot}; do bash "$root/Scripts/Tests/verify-deployment.sh" "$mode" "$workspace/output" "$framework"; done
dotnet publish "$workspace/dbus/DBusAot.csproj" -c Release -r linux-x64 --self-contained true -p:PublishAot=true -p:NazorEnablePublishValidation=true -p:NazorDeploymentFramework="$framework" -p:NuGetAudit=false -o "$workspace/dbus-output" > "$workspace/dbus-build.log" 2>&1
dbus-run-session -- timeout 30s "$workspace/dbus-output/DBusAot" > "$workspace/dbus-run.log" 2>&1
grep -q 'PASS: D-Bus' "$workspace/dbus-run.log"
echo "PASS: packaged Linux $framework window, single-file, AOT and D-Bus. Evidence: $workspace"
