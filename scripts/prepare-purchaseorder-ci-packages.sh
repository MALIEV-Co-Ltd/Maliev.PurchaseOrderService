#!/usr/bin/env bash
set -euo pipefail

readonly messaging_source="${1:-.ci-sources/Maliev.MessagingContracts}"
readonly aspire_source="${2:-.ci-sources/Maliev.Aspire}"
readonly output_path="${3:-.ci-packages}"
readonly messaging_commit="559a00db0c7920a5247fdff60d4476ad23a9a501"
readonly aspire_commit="25a5c3b2d3d6b5ce8ed485d2d44a28f4dc4c9b51"
readonly messaging_version="1.0.96-alpha"
readonly service_defaults_version="1.0.89-alpha"
readonly ci_nuget_config="$(pwd)/NuGet.PRValidation.Config"

assert_checkout() {
  local actual_commit
  actual_commit="$(git -C "$1" rev-parse HEAD)"
  [[ "$actual_commit" == "$2" ]] || { printf 'Expected %s at %s, found %s\n' "$1" "$2" "$actual_commit" >&2; exit 1; }
}
assert_checkout "$messaging_source" "$messaging_commit"
assert_checkout "$aspire_source" "$aspire_commit"

mkdir -p -- "$output_path"
find "$output_path" -maxdepth 1 -type f \( -name '*.nupkg' -o -name '*.snupkg' -o -name 'SHA256SUMS' \) -delete
readonly output_dir="$(cd "$output_path" && pwd)"
readonly generator_project="$messaging_source/tools/Generator/Generator.csproj"
readonly messaging_project="$messaging_source/generated/csharp/Maliev.MessagingContracts.csproj"
readonly defaults_project="$aspire_source/Maliev.Aspire.ServiceDefaults/Maliev.Aspire.ServiceDefaults.csproj"

dotnet restore "$generator_project" --configfile "$ci_nuget_config"
(cd "$messaging_source" && dotnet run --project tools/Generator/Generator.csproj --configuration Release --no-restore)
dotnet restore "$messaging_project" --configfile "$ci_nuget_config"
dotnet pack "$messaging_project" --configuration Release --no-restore -p:NoWarn=CS1570 -p:PackageVersion="$messaging_version" --output "$output_dir"
dotnet restore "$defaults_project" --configfile "$ci_nuget_config" -p:GITHUB_ACTIONS=true -p:SharedLibraryVersion="$messaging_version"
dotnet pack "$defaults_project" --configuration Release --no-restore -p:GITHUB_ACTIONS=true -p:SharedLibraryVersion="$messaging_version" -p:PackageVersion="$service_defaults_version" --output "$output_dir"

test -s "$output_dir/Maliev.MessagingContracts.$messaging_version.nupkg"
test -s "$output_dir/Maliev.Aspire.ServiceDefaults.$service_defaults_version.nupkg"
(cd "$output_dir" && sha256sum "Maliev.MessagingContracts.$messaging_version.nupkg" "Maliev.Aspire.ServiceDefaults.$service_defaults_version.nupkg" > SHA256SUMS && sha256sum --check SHA256SUMS)
