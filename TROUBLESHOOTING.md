# FastFSM Build System - Troubleshooting Guide

## Quick Diagnostics

Run this first to check your environment:

```powershell
# Check environment
Write-Host "Current Path: $($PWD.Path)"
Write-Host "Is WSL Path: $($PWD.Path -like '*\\wsl*')"
Write-Host "WSL Available: $((Get-Command wsl.exe -ErrorAction SilentlyContinue) -ne $null)"
Write-Host "Local Feed: $env:LOCALAPPDATA\FastFsm\nuget"
Write-Host "Feed Exists: $(Test-Path '$env:LOCALAPPDATA\FastFsm\nuget')"

# Check WSL distros
wsl.exe -l -v

# Check dotnet
dotnet --version
```

## Common Issues and Solutions

### 1. NU1301: Invalid URI Error

**Error Message:**
```
error NU1301: Invalid URI: The hostname could not be parsed
```

**Cause:** You're trying to use old scripts or direct dotnet commands with WSL paths.

**Solution:**
```powershell
# DON'T do this:
cd \\wsl.localhost\Ubuntu\home\user\FastFsm
dotnet build  # Will fail!

# DO this instead:
cd \\wsl.localhost\Ubuntu\home\user\FastFsm
.\build.ps1  # Uses delegation to avoid UNC
```

### 2. WSL Not Found

**Error Message:**
```
wsl.exe: command not found
The term 'wsl.exe' is not recognized
```

**Solutions:**

**Option A: Install WSL (Windows)**
```powershell
wsl --install
# Restart computer
wsl --set-default-version 2
wsl --install -d Ubuntu-24.04
```

**Option B: Use Windows-only build**
```powershell
# Clone to Windows location
cd C:\Repos
git clone https://github.com/user/FastFsm.git
cd FastFsm
.\build.ps1  # Will use Windows strategy
```

### 3. Package Not Found During Restore

**Error Message:**
```
error NU1101: Unable to find package FastFsm.Net
```

**Diagnosis:**
```powershell
# Check if packages exist
dir "$env:LOCALAPPDATA\FastFsm\nuget\*.nupkg"

# Check package sources
dotnet nuget list source
```

**Solutions:**

**Solution 1: Rebuild packages**
```powershell
.\build.ps1 -Clean -PackOnly
```

**Solution 2: Clear caches**
```powershell
dotnet nuget locals all --clear
Remove-Item "$env:LOCALAPPDATA\FastFsm\nuget\*.nupkg" -Force
.\build.ps1 -Clean
```

**Solution 3: Check version mismatch**
```powershell
# Check version.json
Get-Content version.json

# Check what version projects expect
Select-String -Path "**\*.csproj" -Pattern "PackageReference.*FastFsm"
```

### 4. Access Denied to /mnt/c

**Error Message (in WSL):**
```
Permission denied: /mnt/c/Users/.../AppData/Local/FastFsm/nuget
```

**Solutions:**

**Fix permissions:**
```bash
# In WSL
sudo chmod 755 /mnt/c
ls -la /mnt/c/Users/$USER/AppData/Local/

# If needed, fix WSL mount options
sudo nano /etc/wsl.conf
# Add:
[automount]
options = "metadata,umask=022"

# Restart WSL
wsl.exe --shutdown
```

**Windows side check:**
```powershell
# Check Windows permissions
icacls "$env:LOCALAPPDATA\FastFsm"

# Fix if needed
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\FastFsm\nuget"
```

### 5. JSON Parsing Error

**Error Message:**
```
ConvertFrom-Json: Conversion from JSON failed with error: Unexpected character encountered
```

**Cause:** Malformed version.json

**Fix:**
```powershell
# Validate JSON
Get-Content version.json -Raw | Test-Json

# Common issue: missing comma
# BAD:
{
  "version": "0.8.0"
  "buildNumber": 5
}

# GOOD:
{
  "version": "0.8.0",
  "buildNumber": 5
}
```

**Reset version.json:**
```powershell
@'
{
  "version": "0.8.0",
  "suffix": "dev",
  "buildNumber": 1,
  "autoIncrement": true
}
'@ | Set-Content version.json -Encoding UTF8
```

### 6. Build Works But VS2022 Can't Find Packages

**Symptoms:**
- `.\build.ps1` succeeds
- Visual Studio shows package errors

**Solutions:**

**1. Clear VS cache:**
```powershell
# Close VS2022 first
Remove-Item "$env:LOCALAPPDATA\Microsoft\VisualStudio\17.0*\ComponentModelCache" -Recurse -Force
```

**2. Check VS package sources:**
- Tools → NuGet Package Manager → Package Manager Settings
- Package Sources
- Ensure local feed is listed

**3. Add via Package Manager Console:**
```powershell
# In VS Package Manager Console
dotnet nuget add source "$env:LOCALAPPDATA\FastFsm\nuget" -n "FastFsm Local"
```

### 7. Wrong WSL Distribution Detected

**Error Message:**
```
WSL distro 'Ubuntu-24.04' not found
```

**Diagnosis:**
```powershell
# List all distros
wsl.exe -l -v

# Check default
wsl.exe -l
```

**Solutions:**

**Set correct distro:**
```powershell
# Option 1: Set as default
wsl.exe --set-default Ubuntu-22.04

# Option 2: Edit build.ps1 line 67
$wslDistro = "Ubuntu-22.04"  # Change to your distro

# Option 3: Use parameter (if implemented)
.\build.ps1 -WslDistro "Ubuntu-22.04"
```

### 8. Tests Fail But Build Succeeds

**Common causes:**

**1. Missing test dependencies:**
```powershell
# Ensure all test projects are restored
dotnet restore FastFsm.Tests\FastFsm.Tests.csproj
```

**2. Path issues in tests:**
```csharp
// Bad: Hardcoded paths
var path = @"C:\temp\test.txt";

// Good: Relative or temp paths
var path = Path.Combine(Path.GetTempPath(), "test.txt");
```

**3. Local feed not available to tests:**
```powershell
# Set environment variable
$env:FASTFSM_LOCAL_FEED = "$env:LOCALAPPDATA\FastFsm\nuget"
.\build.ps1
```

### 9. Sync Script Issues (sync-wsl-to-windows.ps1)

**rsync not found:**
```bash
# In WSL
sudo apt-get update
sudo apt-get install -y rsync
```

**inotify limit reached:**
```bash
# Increase inotify watches
echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

### 10. Performance Issues

**Slow builds from WSL paths:**

**Diagnosis:**
```powershell
Measure-Command { .\build.ps1 }
```

**Solutions:**

**1. Use sync script for native speed:**
```powershell
.\sync-wsl-to-windows.ps1
cd $env:USERPROFILE\source\repos\FastFsm.windows
.\build.ps1  # Much faster
```

**2. Disable Windows Defender for dev folders:**
```powershell
# As Administrator
Add-MpPreference -ExclusionPath "$env:LOCALAPPDATA\FastFsm"
Add-MpPreference -ExclusionPath "\\wsl.localhost"
```

**3. Use RAM disk for packages:**
```powershell
# Create RAM disk (requires third-party tool)
# Then set feed location
$env:FASTFSM_LOCAL_FEED = "R:\FastFsm\nuget"
```

## Advanced Debugging

### Enable Verbose Logging

```powershell
# PowerShell verbose
$VerbosePreference = "Continue"
.\build.ps1

# Dotnet detailed verbosity
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
dotnet build -v diag > build.log 2>&1
```

### Trace Package Resolution

```powershell
# See exactly where packages are loaded from
dotnet build -v normal | Select-String "Resolving"
```

### Check File System Boundaries

```powershell
# Test WSL access from Windows
Test-Path "\\wsl.localhost\Ubuntu\home"
dir "\\wsl.localhost\Ubuntu\home"

# Test Windows access from WSL
wsl.exe bash -c "ls -la /mnt/c/Users"
```

### Network Diagnostics

```powershell
# Check if WSL networking works
wsl.exe bash -c "ping -c 1 google.com"
wsl.exe bash -c "curl -I https://api.nuget.org/v3/index.json"
```

## Reset Everything

If all else fails, complete reset:

```powershell
# 1. Clear all caches
dotnet nuget locals all --clear

# 2. Remove local packages
Remove-Item "$env:LOCALAPPDATA\FastFsm" -Recurse -Force -ErrorAction SilentlyContinue

# 3. Reset WSL (nuclear option)
wsl.exe --shutdown
# or even
wsl.exe --unregister Ubuntu-24.04
wsl.exe --install -d Ubuntu-24.04

# 4. Fresh clone
cd C:\Repos
git clone https://github.com/user/FastFsm.git
cd FastFsm

# 5. Clean build
.\build.ps1 -Clean
```

## Prevention Best Practices

### DO:
- Always use `build.ps1` from repo root
- Keep version.json valid JSON
- Use absolute paths in scripts
- Clear caches when switching branches

### DON'T:
- Run `dotnet` commands directly on WSL paths
- Edit the temporary nuget.config files
- Mix packages from different branches
- Use relative paths in build scripts

## Getting Help

### Diagnostic Information to Provide

When reporting issues, include:

```powershell
# Run this diagnostic script
@"
Environment Report
==================
Date: $(Get-Date)
User: $env:USERNAME
Computer: $env:COMPUTERNAME

Path: $($PWD.Path)
Is WSL: $($PWD.Path -like '*\\wsl*')

WSL Status:
$((wsl.exe -l -v) -join "`n")

Dotnet Version:
$(dotnet --version)

Local Feed:
$(Test-Path "$env:LOCALAPPDATA\FastFsm\nuget")
$(dir "$env:LOCALAPPDATA\FastFsm\nuget\*.nupkg" -ErrorAction SilentlyContinue | Select-Object Name, Length, LastWriteTime)

Version.json:
$(Get-Content version.json -ErrorAction SilentlyContinue)

Last Error:
$($Error[0])
"@ | Out-File diagnostic.txt
Write-Host "Diagnostic info saved to diagnostic.txt"
```

### Support Channels

1. GitHub Issues: [FastFsm/issues](https://github.com/user/FastFsm/issues)
2. Team Chat: #fastfsm-build
3. Email: build-support@fastfsm.com

## Quick Reference Card

```
┌─────────────────────────────────────────────────┐
│            FastFSM Build Quick Fixes            │
├─────────────────────────────────────────────────┤
│ NU1301 Error      → Use build.ps1, not dotnet  │
│ Package not found → .\build.ps1 -Clean         │
│ Access denied     → Check Windows permissions   │
│ WSL not found     → wsl --install              │
│ JSON error        → Fix version.json commas    │
│ VS can't find pkg → Clear VS cache             │
│ Slow builds       → Use sync-wsl-to-windows.ps1│
│ Reset everything  → Clear caches + fresh clone │
└─────────────────────────────────────────────────┘
```

Remember: The build.ps1 script is designed to handle all the complexity. When in doubt, use it instead of direct dotnet commands!