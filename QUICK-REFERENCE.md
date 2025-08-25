# FastFSM Build System - Quick Reference

## One Command to Rule Them All

```powershell
.\build.ps1
```

This works from **anywhere** - WSL path, Windows path, VS2022, PowerShell, Terminal.

## Essential Commands

### Basic Build Operations

```powershell
# Standard build (auto-detects location)
.\build.ps1

# Debug configuration
.\build.ps1 -Configuration Debug

# Skip tests (faster)
.\build.ps1 -SkipTests

# Pack only (create NuGet packages)
.\build.ps1 -PackOnly

# Clean build (remove all artifacts first)
.\build.ps1 -Clean

# Combined options
.\build.ps1 -Clean -Configuration Debug -SkipTests
```

### WSL-Specific Operations

```bash
# From WSL terminal
cd ~/FastFsm
./build-and-test.sh

# Pack to Windows location
./build-and-test.sh --pack-only --out /mnt/c/Users/$USER/AppData/Local/FastFsm/nuget

# Skip version increment
./build-and-test.sh --no-increment
```

### Optional Sync Tool

```powershell
# One-time sync WSL → Windows
.\sync-wsl-to-windows.ps1

# Continuous watch mode
.\sync-wsl-to-windows.ps1 -Watch

# Custom target
.\sync-wsl-to-windows.ps1 -WinRepo "D:\Projects\FastFsm"
```

## Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| **NU1301 Error** | Use `.\build.ps1` not `dotnet build` |
| **Package not found** | `.\build.ps1 -Clean -PackOnly` |
| **Access denied** | `New-Item -Force -Path "$env:LOCALAPPDATA\FastFsm\nuget"` |
| **WSL not found** | `wsl --install` |
| **JSON parse error** | Fix commas in version.json |
| **VS can't find packages** | Clear ComponentModelCache |
| **Slow from WSL path** | Use `sync-wsl-to-windows.ps1` |

## Key Locations

### Windows
```
C:\Users\{username}\AppData\Local\FastFsm\nuget\  # Local packages
%TEMP%\fastfsm.nuget.temp.config                   # Temp config
```

### WSL
```
/home/{username}/FastFsm/                          # Repository
/mnt/c/Users/{username}/AppData/Local/FastFsm/     # Package output
```

## Environment Variables

```powershell
# Use custom local feed location
$env:FASTFSM_LOCAL_FEED = "D:\MyPackages"
.\build.ps1

# Check current settings
Write-Host "Feed: $env:LOCALAPPDATA\FastFsm\nuget"
Write-Host "Custom: $env:FASTFSM_LOCAL_FEED"
```

## Common Workflows

### 1. Fresh Start
```powershell
git clone https://github.com/user/FastFsm.git
cd FastFsm
.\build.ps1
```

### 2. After Pulling Changes
```powershell
git pull
.\build.ps1 -Clean
```

### 3. Creating New Package Version
```powershell
# Auto-increments build number
.\build.ps1 -PackOnly

# Check created packages
dir "$env:LOCALAPPDATA\FastFsm\nuget\*.nupkg" | Sort-Object LastWriteTime -Desc | Select -First 3
```

### 4. Debug Failed Tests
```powershell
# Build debug configuration
.\build.ps1 -Configuration Debug

# Open in VS2022 and debug
start .\FastFsm.Net.slnx
```

### 5. CI/CD Build
```yaml
# GitHub Actions
- run: |
    if ($IsWindows) { 
      .\build.ps1 
    } else { 
      ./build-and-test.sh 
    }
```

## Architecture at a Glance

```
Your Location          Script Action
─────────────          ─────────────
\\wsl.localhost\...  → Delegates to WSL → No UNC errors!
C:\Repos\...        → Builds directly  → Native speed!
/home/user/...      → Uses bash script → Linux native!

All write to: C:\Users\...\AppData\Local\FastFsm\nuget\
```

## Decision Tree

```
Q: Where is my code?
├─ WSL (\\wsl.localhost\...)
│  └─ Use: .\build.ps1 (auto-delegates)
├─ Windows (C:\...)
│  └─ Use: .\build.ps1 (builds locally)
└─ Pure Linux/WSL terminal
   └─ Use: ./build-and-test.sh

Q: What do I want to do?
├─ Just build → .\build.ps1
├─ Create packages → .\build.ps1 -PackOnly
├─ Clean everything → .\build.ps1 -Clean
├─ Debug in VS2022 → .\build.ps1 -Configuration Debug
└─ Quick iteration → .\build.ps1 -SkipTests
```

## Golden Rules

1. **Always use `build.ps1`** - It handles all complexity
2. **Never use `dotnet` directly on WSL paths** - Causes NU1301
3. **Local feed is shared** - Same packages for WSL and Windows
4. **Path detection is automatic** - Script knows what to do
5. **Temporary configs are safe** - Never modify global settings

## Emergency Reset

```powershell
# Nuclear option - reset everything
dotnet nuget locals all --clear
Remove-Item "$env:LOCALAPPDATA\FastFsm" -Recurse -Force
git clean -xfd
.\build.ps1 -Clean
```

## Visual Studio 2022 Integration

1. **Open from anywhere**: File → Open → Open Project/Solution
2. **Navigate to**: `\\wsl.localhost\Ubuntu\home\user\FastFsm\FastFsm.Net.slnx`
3. **Build**: Ctrl+Shift+B (uses packages from local feed)
4. **Test**: Ctrl+R, A (runs all tests)
5. **Debug**: F5 (works normally)

## Package Versioning

```json
// version.json
{
  "version": "0.8.0",      // Major.Minor.Patch
  "suffix": "dev",          // Pre-release suffix
  "buildNumber": 5,         // Auto-incremented
  "autoIncrement": true     // Enable auto-increment
}

// Results in: 0.8.0.5-dev
```

## Command Cheat Sheet

```powershell
# Most common commands
.\build.ps1                           # Build everything
.\build.ps1 -SkipTests               # Build without tests
.\build.ps1 -PackOnly                # Create packages only
.\build.ps1 -Clean                   # Clean build
.\build.ps1 -Configuration Debug     # Debug build

# Diagnostics
wsl.exe -l -v                        # List WSL distros
dotnet --version                     # Check dotnet version
Test-Path "$env:LOCALAPPDATA\FastFsm\nuget"  # Check feed exists

# Cleanup
dotnet nuget locals all --clear      # Clear all caches
Remove-Item "$env:LOCALAPPDATA\FastFsm\nuget\*.nupkg"  # Remove packages

# Package management
dir "$env:LOCALAPPDATA\FastFsm\nuget\*.nupkg"  # List packages
dotnet nuget list source             # Show package sources
```

## Success Indicators

✅ **Good output:**
```
=== FastFSM Universal Build Script ===
→ Strategy: Delegate to WSL
✓ WSL build completed
✓ Build completed successfully!
```

❌ **Bad output:**
```
error NU1301: Invalid URI: The hostname could not be parsed
```
**Fix:** Use `.\build.ps1` instead of direct `dotnet` commands

## Remember

> **The build.ps1 script is your friend.** It detects where you are, chooses the right strategy, and avoids all UNC path issues automatically. When in doubt, just run `.\build.ps1`!

---
*FastFSM Portable Build System v2.0 - "It Just Works!"*