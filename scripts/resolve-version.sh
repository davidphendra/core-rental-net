#!/usr/bin/env bash
# Resolves the release version from the tag a pipeline is building.
#
# One rule, called by both the application pipeline and the agent pipeline, so the two deployables
# cannot disagree about which release they are shipping. The build never reads Git: the pipeline
# resolves the tag and passes the result in, which is why this is a script and not a build task.
#
# Emits Azure Pipelines variables:
#   version              the release, e.g. 1.4.0 or 1.4.0-rc.1
#   informationalVersion the release plus the commit as build metadata, e.g. 1.4.0+abc1234
#   shortSha             the seven-character commit the artifact was built from
set -euo pipefail

floor="${VERSION_FLOOR:-0.1.0}"
reference="${BUILD_SOURCEBRANCH:-}"

# refs/tags/v1.4.0 -> 1.4.0. A branch ref leaves a value that fails the check below and takes the floor.
tag="${reference#refs/tags/}"
tag="${tag#v}"

if [[ "$tag" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]]; then
  version="$tag"
else
  version="$floor"
fi

sha="${BUILD_SOURCEVERSION:-0000000}"
shortSha="${sha:0:7}"
informationalVersion="$version+$shortSha"

echo "Resolved version=$version commit=$shortSha"
echo "##vso[task.setvariable variable=version]$version"
echo "##vso[task.setvariable variable=informationalVersion]$informationalVersion"
echo "##vso[task.setvariable variable=shortSha]$shortSha"
