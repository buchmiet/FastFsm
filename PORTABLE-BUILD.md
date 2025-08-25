# Portable Build System

## Overview
This repository is fully portable - it works identically whether cloned in WSL or on Windows. No manual configuration needed!

## Quick Start

### From anywhere (WSL or Windows):
```powershell
.\build.ps1
```

That's it! The script automatically detects where you are and does the right thing.

## How It Works

### Core Principle: No UNC Paths in NuGet/dotnet
The build system **never** uses `\\wsl$\...` paths with NuGet or dotnet commands, avoiding the "UNC paths are not supported" errors.

### Architecture

```
┌─────────────────────────────────────┐
│         build.ps1 (universal)       │
│                                     │
│  Detects current location:          │
│  • WSL path? → Delegate to WSL      │
│  • Windows path? → Build locally    │
└─────────────┬───────────────────────┘
              │
    ┌─────────┴─────────┐
    ▼                   ▼
┌──────────┐      ┌──────────┐
│   WSL    │      │ Windows  │
│          │      │          │
│ Runs:    │      │ Runs:    │
│ bash     │      │ dotnet   │
│ script   │      │ directly │
└────┬─────┘      └────┬─────┘
     │                 │
     └────────┬────────┘
              ▼
    C:\Users\...\AppData\
    Local\FastFsm\nuget
    (shared local feed)
```

### Strategies

#### Strategy 1: Running from WSL path (\\wsl$\...)
- Script detects WSL path
- Delegates to `wsl.exe` to run `build-and-test.sh`
- WSL writes packages to Windows via `/mnt/c/...`
- No UNC paths used

#### Strategy 2: Running from Windows path (C:\...)
- Script runs dotnet commands directly
- Uses temporary nuget.config with local folder feed
- Standard Windows build process

## Scripts

### build.ps1 (Universal)
Works from anywhere:
```powershell
# Basic usage
.\build.ps1

# Debug configuration
.\build.ps1 -Configuration Debug

# Skip tests
.\build.ps1 -SkipTests

# Pack only
.\build.ps1 -PackOnly

# Clean build
.\build.ps1 -Clean
```

### build-and-test.sh (WSL)
Enhanced for cross-platform support:
```bash
# Standard usage
./build-and-test.sh

# Pack to Windows location
./build-and-test.sh --pack-only --out /mnt/c/Users/$USER/AppData/Local/FastFsm/nuget
```

### sync-wsl-to-windows.ps1 (Optional)
For those who prefer a Windows-native copy:
```powershell
# One-time sync
.\sync-wsl-to-windows.ps1

# Continuous sync
.\sync-wsl-to-windows.ps1 -Watch

# Custom target
.\sync-wsl-to-windows.ps1 -WinRepo "D:\Projects\FastFsm"
```

## File Locations

### Shared Local Feed
All builds (WSL and Windows) use the same local feed:
```
C:\Users\{username}\AppData\Local\FastFsm\nuget\
```

In WSL, this is accessed as:
```
/mnt/c/Users/{username}/AppData/Local/FastFsm/nuget/
```

### Repository Location
Can be anywhere:
- `\\wsl.localhost\Ubuntu-24.04\home\user\FastFsm` ✓
- `C:\Repos\FastFsm` ✓
- `D:\Projects\FastFsm` ✓
- `/home/user/FastFsm` (in WSL) ✓

## Configuration Files

### nuget.config (minimal)
Repository contains only:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Local feeds are added temporarily via `--configfile` during builds.

### Directory.Build.props
Supports optional `FASTFSM_LOCAL_FEED` environment variable (already configured).

## Workflows

### Developer on Windows with WSL
1. Clone repo in WSL: `git clone https://github.com/user/FastFsm.git`
2. Open in Windows: `explorer.exe .`
3. Run build: `.\build.ps1`
4. Open in VS: Just open the .slnx file

### Developer on pure Windows
1. Clone repo: `git clone https://github.com/user/FastFsm.git`
2. Run build: `.\build.ps1`
3. Open in VS: Just open the .slnx file

### Developer in WSL/Linux
1. Clone repo: `git clone https://github.com/user/FastFsm.git`
2. Run build: `./build-and-test.sh`

### CI/CD Pipeline
```yaml
# GitHub Actions example
- name: Build on Windows
  run: .\build.ps1
  
- name: Build on Linux
  run: ./build-and-test.sh
```

## Troubleshooting

### "wsl.exe not found"
- You're on pure Linux (not WSL) - use `./build-and-test.sh` directly
- Or install WSL on Windows

### "Access denied"
- Check permissions on `%LOCALAPPDATA%\FastFsm\nuget`
- Run PowerShell as Administrator if needed

### Package conflicts
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Delete `%LOCALAPPDATA%\FastFsm\nuget\*.nupkg`

### Can't find solution
- Ensure you're running from repository root
- Check that `.slnx` file exists

## Design Principles

1. **Zero Configuration**: Clone and build, no setup needed
2. **No UNC in NuGet**: Avoids all UNC-related issues
3. **Shared Feed**: One local feed for all scenarios
4. **Portable Scripts**: Same commands work everywhere
5. **VS-Friendly**: Works seamlessly with Visual Studio 2022

## Why This Works

- **Folder feeds are standard**: NuGet fully supports local folder feeds ([docs](https://learn.microsoft.com/en-us/nuget/hosting-packages/local-feeds))
- **WSL interop is stable**: `/mnt/c` access is officially supported ([docs](https://learn.microsoft.com/en-us/windows/wsl/filesystems))
- **Temporary configs are safe**: `--configfile` doesn't modify global settings
- **Detection is reliable**: PowerShell can identify WSL paths consistently

## Migration from Old System

If you were using the old build system:
1. Delete any mapped network drives to WSL
2. Clear NuGet caches: `dotnet nuget locals all --clear`
3. Use the new `build.ps1` script

## Summary

This portable build system "just works":
- **One script** (`build.ps1`) for all scenarios
- **No manual configuration** needed
- **No UNC path issues** ever
- **Full Visual Studio** compatibility
- **Cross-platform** by design

Just clone and build!