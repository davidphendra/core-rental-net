#!/usr/bin/env bash
# Writes the release version into the agent project's own folder.
#
# Foundry zips that folder and builds it server-side, so the repository root's Directory.Build.props is
# not in the zip. This is how the resolved tag travels with the code instead: the generated file is
# imported by CoreRentalNet.Agents.csproj when it is present.
#
# Usage: write-agent-version.sh <version> <informationalVersion> [destination]
set -euo pipefail

version="${1:?the release version is required}"
informationalVersion="${2:?the informational version is required}"
destination="${3:-agentfoundry/src/CoreRentalNet.Agents/Version.g.props}"

cat > "$destination" <<EOF
<Project>
  <PropertyGroup>
    <Version>$version</Version>
    <InformationalVersion>$informationalVersion</InformationalVersion>
  </PropertyGroup>
</Project>
EOF

echo "Wrote $destination for $informationalVersion"
