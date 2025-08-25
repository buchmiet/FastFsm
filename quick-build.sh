#!/bin/bash
# Quick build script - just builds the package without tests
set -e

# Colors
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}Quick Build - FastFSM${NC}"

# Read version
VERSION=$(grep '"version"' version.json | sed 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')
BUILD=$(grep '"buildNumber"' version.json | sed 's/.*"buildNumber"[[:space:]]*:[[:space:]]*\([0-9]*\).*/\1/')
SUFFIX=$(grep '"suffix"' version.json | sed 's/.*"suffix"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')

# Increment build
BUILD=$((BUILD + 1))
sed -i "s/\"buildNumber\".*:.*[0-9]*/\"buildNumber\": $BUILD/" version.json

FULL_VERSION="${VERSION}.${BUILD}-${SUFFIX}"

echo -e "Building: ${GREEN}$FULL_VERSION${NC}"

# Build package
dotnet pack FastFsm/FastFsm.csproj \
    -c Release \
    -p:PackageVersion="$FULL_VERSION" \
    --output nuget \
    -v quiet

echo -e "${GREEN}✓ Package created: FastFsm.Net.$FULL_VERSION.nupkg${NC}"
echo -e "Location: $(pwd)/nuget"