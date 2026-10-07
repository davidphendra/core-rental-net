#!/usr/bin/env bash
# Writes the agent build identity into the agent project's own folder.
#
# Foundry zips that folder and builds it server-side, so the repository root's Directory.Build.props is
# not in the zip. This is how the resolved identity travels with the code instead: the generated file is
# imported by CoreRentalNet.Agents.csproj when it is present, and the csproj turns each property into an
# assembly metadata attribute. The identity is therefore part of the artifact, not of the environment it
# happens to run in.
#
# Usage: write-agent-version.sh <version> <informationalVersion> [gitSha] [buildId] [environment] [destination]
#
# gitSha, buildId and environment are what the artifact reports about the build that produced it. Each is
# optional because a developer's build has none, and the csproj omits a metadata item that is empty rather
# than stamping an empty identity.
set -euo pipefail

version="${1:?the release version is required}"
informationalVersion="${2:?the informational version is required}"
gitSha="${3:-}"
buildId="${4:-}"
environment="${5:-}"
destination="${6:-agentfoundry/src/CoreRentalNet.Agents/Version.g.props}"

cat > "$destination" <<EOF
<Project>
  <PropertyGroup>
    <Version>$version</Version>
    <InformationalVersion>$informationalVersion</InformationalVersion>
    <GitSha>$gitSha</GitSha>
    <BuildId>$buildId</BuildId>
    <AgentEnvironment>$environment</AgentEnvironment>
  </PropertyGroup>
</Project>
EOF

echo "Wrote $destination for $informationalVersion (gitSha=${gitSha:-unset} buildId=${buildId:-unset} environment=${environment:-unset})"
