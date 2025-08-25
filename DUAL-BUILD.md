# Dual Build System - WSL + Windows

## Overview
This solution enables building FastFSM in a dual environment (WSL + Windows) without any UNC path issues.

## Key Principles
1. **NO UNC paths** (`\\wsl$\...`) in NuGet operations
2. **WSL packs to Windows** via `/mnt/c/...`
3. **Windows consumes from local feed** (`C:\Users\...\AppData\Local\FastFsm\nuget`)
4. **Simple and reliable**

## Architecture

```
WSL (Ubuntu)                          Windows
────────────                          ────────
/home/user/FastFsm                    VS 2022
     │                                    │
     ├─ build-and-test.sh                │
     │   └─ pack to ──────┐              │
     │                     ↓              │
     │              /mnt/c/Users/.../    │
     │                     │              │
     └─────────────────────┼──────────────┘
                           ↓
                    C:\Users\...\AppData\Local\FastFsm\nuget
                           ↑
                           │
                    build-for-vs-simple.ps1
                    (restore/build/test)
```

## Scripts

### WSL: build-and-test.sh
Enhanced with `--pack-only` and `--out` options:

```bash
# Pack only (no tests) to Windows feed
./build-and-test.sh --pack-only --out /mnt/c/Users/$USER/AppData/Local/FastFsm/nuget

# Full build with custom output
./build-and-test.sh --out /custom/path

# Traditional usage still works
./build-and-test.sh
```

Key features:
- Auto-detects Windows username
- Default output: `/mnt/c/Users/{WIN_USER}/AppData/Local/FastFsm/nuget`
- Creates temp NuGet config for local feed during tests

### Windows: build-for-vs-simple.ps1
Simple, reliable script for VS2022:

```powershell
# Default usage (packs in WSL, then builds/tests in Windows)
.\build-for-vs-simple.ps1

# Skip WSL packing (use existing packages)
.\build-for-vs-simple.ps1 -SkipPack

# Quick mode (skip tests)
.\build-for-vs-simple.ps1 -QuickMode

# Custom WSL distro/paths
.\build-for-vs-simple.ps1 `
    -WslDistro "Ubuntu-22.04" `
    -WslRepo "/home/myuser/FastFsm" `
    -WinFeed "D:\MyPackages"
```

Process:
1. Calls WSL to pack (via `wsl.exe`)
2. Creates temp NuGet config
3. Restores from local feed
4. Builds solution
5. Runs tests

## File Structure

```
Windows:
C:\Users\{username}\
└── AppData\Local\FastFsm\nuget\
    ├── FastFsm.Net.0.8.0.5-dev.nupkg
    ├── FastFsm.Net.Logging.0.8.0.5-dev.nupkg
    └── ...

WSL:
/home/{username}/FastFsm/
├── build-and-test.sh
├── version.json
├── nuget.config (minimal - only nuget.org)
└── Directory.Build.props
```

## Configuration Files

### nuget.config (minimal)
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### Directory.Build.props (unchanged)
Already supports `FASTFSM_LOCAL_FEED` environment variable:
```xml
<PropertyGroup>
  <FastFsmLocalFeed>$(FASTFSM_LOCAL_FEED)</FastFsmLocalFeed>
</PropertyGroup>
<PropertyGroup Condition=" '$(FastFsmLocalFeed)' != '' ">
  <RestoreAdditionalProjectSources>
    $(RestoreAdditionalProjectSources);$(FastFsmLocalFeed)
  </RestoreAdditionalProjectSources>
</PropertyGroup>
```

## Usage Scenarios

### Scenario 1: Development in VS2022
```powershell
# From PowerShell/VS Package Manager Console
cd C:\Repos\FastFsm  # Your local clone
.\build-for-vs-simple.ps1
# Now debug tests in VS2022
```

### Scenario 2: WSL-only development
```bash
# In WSL
cd ~/FastFsm
./build-and-test.sh  # Full build and test
```

### Scenario 3: CI/CD Pipeline
```bash
# WSL/Linux agent - just pack
./build-and-test.sh --pack-only --out /artifacts

# Windows agent - consume packages
dotnet restore --source /artifacts
dotnet build
dotnet test
```

### Scenario 4: Quick iteration
```powershell
# After making changes, quick rebuild
.\build-for-vs-simple.ps1 -SkipPack -QuickMode
```

## Troubleshooting

### "wsl.exe: command not found"
- Ensure WSL is installed and configured
- Check WSL distro name: `wsl -l -v`

### "Package not found"
- Check if packages exist in Windows feed
- Clear NuGet cache: `dotnet nuget locals all --clear`

### "Access denied" to /mnt/c
- Check Windows permissions
- Ensure path exists and is writable

### Version conflicts
- Check `version.json` is in sync
- Use `--no-increment` flag if needed

## Benefits

1. **No UNC issues** - completely avoided
2. **Native performance** - each OS uses native paths
3. **Simple scripts** - easy to understand and maintain
4. **Flexible** - works for various workflows
5. **VS2022 friendly** - seamless debugging experience

## Migration from Old Scripts

If you were using the complex `build-and-test.ps1` with pushd/popd:

1. Replace with `build-for-vs-simple.ps1`
2. Remove any `net use` mappings
3. Clear NuGet caches
4. Use the new workflow

## Summary

This dual-build system provides a clean separation:
- **WSL handles packing** → outputs to Windows via `/mnt/c`
- **Windows handles consumption** → reads from local `C:\` path
- **No UNC paths anywhere** → no parsing errors

Simple, reliable, and maintainable!