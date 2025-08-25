# FastFSM Build System

This document describes the build and test system for FastFSM.

## Quick Start

### Windows (PowerShell / Visual Studio 2022)
```powershell
# Full build and test
.\build-and-test.ps1

# Build only (skip tests)
.\build-and-test.ps1 -SkipTests

# Debug configuration
.\build-and-test.ps1 -Configuration Debug

# Quick build for VS2022
.\build-for-vs.ps1
```

### Linux/Mac (Bash)
```bash
# Full build and test
./build-and-test.sh

# Build only (skip tests)
./build-and-test.sh --skip-tests

# Debug configuration
./build-and-test.sh -c Debug

# Quick build
./quick-build.sh
```

## Version Management

Version information is stored in `version.json`:

```json
{
  "version": "0.8.0",      // Major.Minor.Patch
  "suffix": "dev",         // Pre-release suffix (dev, beta, rc, etc.)
  "buildNumber": 1,        // Auto-incremented on each build
  "autoIncrement": true    // Enable/disable auto-increment
}
```

The full version format is: `{version}.{buildNumber}-{suffix}`
Example: `0.8.0.15-dev`

## Build Scripts

### build-and-test.ps1 / build-and-test.sh

Main build script that:
1. Increments version number
2. Builds NuGet packages
3. Updates test projects to use new packages
4. Runs all tests
5. Reports results

**Options:**
- `-Configuration` / `-c`: Build configuration (Debug/Release)
- `-SkipTests` / `--skip-tests`: Skip running tests
- `-Clean` / `--clean`: Clean before building
- `-Version` / `-v`: Override version
- `-NoIncrement` / `--no-increment`: Don't auto-increment build number

### build-for-vs.ps1

Optimized for Visual Studio 2022 development:
- Quickly builds packages in Debug mode
- Updates test projects
- Prepares for debugging in VS

### quick-build.sh

Minimal script for rapid iteration:
- Just builds the package
- No tests
- Minimal output

## Project Structure

```
FastFsm/
├── version.json                 # Version configuration
├── nuget.config                # NuGet sources configuration
├── Directory.Build.props       # Common MSBuild properties
├── build-and-test.ps1         # Windows build script
├── build-and-test.sh          # Linux/Mac build script
├── build-for-vs.ps1           # VS2022 quick build
├── quick-build.sh             # Minimal build script
├── nuget/                     # Local NuGet packages output
│   ├── FastFsm.Net.*.nupkg
│   └── FastFsm.Net.Logging.*.nupkg
├── FastFsm/                   # Main library project
├── FastFsm.Tests/             # Unit tests
├── FastFsm.Async.Tests/       # Async tests
└── FastFsm.Logging.Tests/     # Logging tests
```

## Visual Studio 2022 Integration

1. Open solution in VS2022
2. Run `.\build-for-vs.ps1` in Package Manager Console
3. Tests are now ready to debug with F5

The script automatically:
- Builds debug packages
- Updates test project references
- Clears NuGet caches
- Restores packages

## NuGet Package Structure

### FastFsm.Net
Main package containing:
- Core state machine runtime
- Source generators
- Abstractions
- Global usings via .props file

### FastFsm.Net.Logging
Optional logging integration:
- ILogger support
- Structured logging
- Performance metrics

## Testing

Tests use the locally built NuGet packages, not project references. This ensures:
- Tests validate the actual package
- No missing dependencies
- Correct package structure

To run tests:
```bash
# All tests
dotnet test

# Specific test project
dotnet test FastFsm.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Troubleshooting

### "Package not found" errors
```bash
# Clear all NuGet caches
dotnet nuget locals all --clear

# Force restore
dotnet restore --force --no-cache
```

### Line ending issues on Linux/Mac
```bash
dos2unix *.sh
# or
sed -i 's/\r$//' *.sh
```

### Version conflicts
Check that all test projects reference the same package version.
The build scripts automatically update these references.

## CI/CD Integration

For CI/CD pipelines, use the `--no-increment` flag to control versioning:

```bash
# Use external version
./build-and-test.sh --version "1.0.0" --no-increment

# Skip tests in CI build step
./build-and-test.sh --skip-tests --no-increment
```

## Contributing

When adding new test projects:
1. Add project path to TEST_PROJECTS array in build scripts
2. Ensure it references FastFsm.Net package (not project)
3. Use version wildcard: `Version="0.8.0.*-dev"`