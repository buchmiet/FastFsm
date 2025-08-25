#!/bin/bash

# FastFSM Build and Test Script
# Builds NuGet packages and runs tests with proper versioning

set -e  # Exit on error

# Default values
CONFIGURATION="Release"
SKIP_TESTS=false
CLEAN=false
VERSION=""
NO_INCREMENT=false
PACK_ONLY=false
OUT_DIR=""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Helper functions
write_success() { echo -e "${GREEN}$*${NC}"; }
write_info() { echo -e "${CYAN}$*${NC}"; }
write_warning() { echo -e "${YELLOW}$*${NC}"; }
write_error() { echo -e "${RED}$*${NC}"; }
write_header() {
    echo ""
    echo -e "${BLUE}════════════════════════════════════════════════════════${NC}"
    echo -e "${WHITE}  $*${NC}"
    echo -e "${BLUE}════════════════════════════════════════════════════════${NC}"
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -s|--skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        --no-increment)
            NO_INCREMENT=true
            shift
            ;;
        --pack-only)
            PACK_ONLY=true
            SKIP_TESTS=true
            shift
            ;;
        --out)
            OUT_DIR="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  -c, --configuration <Debug|Release>  Build configuration (default: Release)"
            echo "  -s, --skip-tests                     Skip running tests"
            echo "  --clean                              Clean before building"
            echo "  -v, --version <version>              Override version"
            echo "  --no-increment                       Don't increment build number"
            echo "  --pack-only                          Only pack NuGet packages (implies --skip-tests)"
            echo "  --out <dir>                          Output directory for packages"
            echo "  -h, --help                           Show this help"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Determine output directory
if [ -z "$OUT_DIR" ]; then
    # Default: output to Windows feed via /mnt/c
    # Detect Windows username from WSL
    WIN_USER=$(cmd.exe /c "echo %USERNAME%" 2>/dev/null | tr -d '\r\n' || echo "$USER")
    OUT_DIR="/mnt/c/Users/${WIN_USER}/AppData/Local/FastFsm/nuget"
    write_info "Using default Windows feed: $OUT_DIR"
fi

# Create output directory
mkdir -p "$OUT_DIR"

write_header "FastFSM Build and Test Script"

# Read version configuration
write_info "Reading version configuration..."
if [ ! -f "version.json" ]; then
    write_error "version.json not found!"
    exit 1
fi

# Parse version.json using basic tools (works on systems without jq)
BASE_VERSION=$(grep '"version"' version.json | sed 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')
SUFFIX=$(grep '"suffix"' version.json | sed 's/.*"suffix"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/')
BUILD_NUMBER=$(grep '"buildNumber"' version.json | sed 's/.*"buildNumber"[[:space:]]*:[[:space:]]*\([0-9]*\).*/\1/')
AUTO_INCREMENT=$(grep '"autoIncrement"' version.json | grep -q "true" && echo "true" || echo "false")

# Override version if provided
if [ -n "$VERSION" ]; then
    BASE_VERSION="$VERSION"
fi

# Increment build number if needed
if [ "$NO_INCREMENT" = false ] && [ "$AUTO_INCREMENT" = "true" ]; then
    BUILD_NUMBER=$((BUILD_NUMBER + 1))
    
    # Update version.json
    sed -i.bak "s/\"buildNumber\"[[:space:]]*:[[:space:]]*[0-9]*/\"buildNumber\": $BUILD_NUMBER/" version.json
    rm -f version.json.bak
    
    write_info "Incremented build number to $BUILD_NUMBER"
fi

# Construct full version
if [ -n "$SUFFIX" ] && [ "$SUFFIX" != "null" ]; then
    FULL_VERSION="${BASE_VERSION}.${BUILD_NUMBER}-${SUFFIX}"
else
    FULL_VERSION="${BASE_VERSION}.${BUILD_NUMBER}"
fi

write_success "Building version: $FULL_VERSION"

# Clean if requested
if [ "$CLEAN" = true ]; then
    write_header "Cleaning Solution"
    dotnet clean -c "$CONFIGURATION" -v minimal || exit 1
    
    # Remove old packages
    write_info "Removing old local packages..."
    rm -f "$OUT_DIR"/FastFsm.Net.*.nupkg
    rm -f "$OUT_DIR"/FastFsm.Net.Logging.*.nupkg
    rm -f "$OUT_DIR"/FastFsm.Net.DependencyInjection.*.nupkg
fi

# Clear NuGet caches
write_header "Clearing NuGet Caches"
write_info "Clearing local NuGet cache..."
dotnet nuget locals temp --clear > /dev/null 2>&1

# Restore packages
write_header "Restoring Packages"
write_info "Restoring solution..."
dotnet restore FastFsm.Net.slnx --force --no-cache

if [ $? -ne 0 ]; then
    write_error "Package restore failed"
    exit 1
fi

# Build FastFsm package
write_header "Building FastFsm NuGet Packages"
write_info "Output directory: $OUT_DIR"

write_info "Building FastFsm.Net package..."
dotnet pack FastFsm/FastFsm.csproj \
    -c "$CONFIGURATION" \
    -p:PackageVersion="$FULL_VERSION" \
    --no-restore \
    --output "$OUT_DIR" \
    -v minimal

if [ $? -eq 0 ]; then
    write_success "✓ Created FastFsm.Net.$FULL_VERSION.nupkg"
else
    write_error "Failed to build FastFsm.Net package"
    exit 1
fi

# Build FastFsm.Logging package if it exists
if [ -f "FastFsm.Logging/FastFsm.Logging.csproj" ]; then
    write_info "Building FastFsm.Net.Logging package..."
    
    dotnet pack FastFsm.Logging/FastFsm.Logging.csproj \
        -c "$CONFIGURATION" \
        -p:PackageVersion="$FULL_VERSION" \
        --no-restore \
        --output "$OUT_DIR" \
        -v minimal
    
    if [ $? -eq 0 ]; then
        write_success "✓ Created FastFsm.Net.Logging.$FULL_VERSION.nupkg"
    else
        write_warning "Failed to build FastFsm.Net.Logging package"
    fi
fi

# Build FastFsm.DependencyInjection package if it exists
if [ -f "FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj" ]; then
    write_info "Building FastFsm.Net.DependencyInjection package..."
    
    dotnet pack FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj \
        -c "$CONFIGURATION" \
        -p:PackageVersion="$FULL_VERSION" \
        --no-restore \
        --output "$OUT_DIR" \
        -v minimal
    
    if [ $? -eq 0 ]; then
        write_success "✓ Created FastFsm.Net.DependencyInjection.$FULL_VERSION.nupkg"
    fi
fi

# If pack-only mode, exit here
if [ "$PACK_ONLY" = true ]; then
    write_header "Pack Complete"
    write_success "Packages created in: $OUT_DIR"
    write_success "Version: $FULL_VERSION"
    ls -la "$OUT_DIR"/*.nupkg 2>/dev/null | tail -3
    exit 0
fi

# Update test projects
write_header "Updating Test Projects"

TEST_PROJECTS=(
    "FastFsm.Tests/FastFsm.Tests.csproj"
    "FastFsm.Async.Tests/FastFsm.Async.Tests.csproj"
    "FastFsm.Logging.Tests/FastFsm.Logging.Tests.csproj"
    "FastFsm.DependencyInjection.Tests/FastFsm.DependencyInjection.Tests.csproj"
)

for TEST_PROJECT in "${TEST_PROJECTS[@]}"; do
    if [ -f "$TEST_PROJECT" ]; then
        write_info "Updating $TEST_PROJECT..."
        
        # Update package references using sed
        sed -i.bak \
            -e "s/<PackageReference Include=\"FastFsm\.Net\" Version=\"[^\"]*\"/<PackageReference Include=\"FastFsm.Net\" Version=\"$FULL_VERSION\"/g" \
            -e "s/<PackageReference Include=\"FastFsm\.Net\.Logging\" Version=\"[^\"]*\"/<PackageReference Include=\"FastFsm.Net.Logging\" Version=\"$FULL_VERSION\"/g" \
            -e "s/<PackageReference Include=\"FastFsm\.Net\.DependencyInjection\" Version=\"[^\"]*\"/<PackageReference Include=\"FastFsm.Net.DependencyInjection\" Version=\"$FULL_VERSION\"/g" \
            "$TEST_PROJECT"
        
        rm -f "${TEST_PROJECT}.bak"
    fi
done

# Create temporary nuget config for local feed
TEMP_CONFIG="/tmp/fastfsm.nuget.config"
cat > "$TEMP_CONFIG" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="LocalFastFsm" value="$OUT_DIR" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# Restore packages with local feed
write_header "Final Restore with Local Packages"
dotnet restore FastFsm.Net.slnx --configfile "$TEMP_CONFIG" --force

# Build solution
write_header "Building Solution"
dotnet build FastFsm.Net.slnx \
    --configfile "$TEMP_CONFIG" \
    --no-restore \
    -c "$CONFIGURATION" \
    -v minimal

if [ $? -ne 0 ]; then
    write_error "Build failed"
    exit 1
fi

# Run tests if not skipped
if [ "$SKIP_TESTS" = false ]; then
    write_header "Running Tests"
    
    FAILED_TESTS=()
    TEST_RESULTS=()
    
    for TEST_PROJECT in "${TEST_PROJECTS[@]}"; do
        if [ -f "$TEST_PROJECT" ]; then
            PROJECT_NAME=$(basename "$TEST_PROJECT" .csproj)
            write_info "Running $PROJECT_NAME..."
            
            if dotnet test "$TEST_PROJECT" \
                --configfile "$TEMP_CONFIG" \
                -c "$CONFIGURATION" \
                --no-build \
                --no-restore \
                --verbosity normal \
                > /tmp/test_output_$$.txt 2>&1; then
                
                write_success "✓ $PROJECT_NAME passed"
                TEST_RESULTS+=("✓ $PROJECT_NAME")
            else
                write_error "✗ $PROJECT_NAME failed"
                FAILED_TESTS+=("$PROJECT_NAME")
                TEST_RESULTS+=("✗ $PROJECT_NAME")
                
                # Show test output on failure
                write_warning "Test output:"
                cat /tmp/test_output_$$.txt | sed 's/^/  /'
            fi
            
            rm -f /tmp/test_output_$$.txt
        fi
    done
    
    # Summary
    write_header "Test Summary"
    for RESULT in "${TEST_RESULTS[@]}"; do
        echo "$RESULT"
    done
    
    if [ ${#FAILED_TESTS[@]} -gt 0 ]; then
        write_error "Tests failed: ${FAILED_TESTS[*]}"
        exit 1
    else
        write_success "All tests passed!"
    fi
fi

# Clean up temp config
rm -f "$TEMP_CONFIG"

# Final summary
write_header "Build Complete"
write_success "Package version: $FULL_VERSION"
write_success "Configuration: $CONFIGURATION"
write_success "Package location: $OUT_DIR"

if [ -f "$OUT_DIR/FastFsm.Net.$FULL_VERSION.nupkg" ]; then
    PACKAGE_SIZE=$(du -h "$OUT_DIR/FastFsm.Net.$FULL_VERSION.nupkg" | cut -f1)
    write_info "Package size: $PACKAGE_SIZE"
fi

echo ""
write_success "Build completed successfully!"