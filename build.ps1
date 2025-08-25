#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Universal build script for FastFSM - works from anywhere
.DESCRIPTION
    This script automatically detects if it's running from WSL path or Windows path
    and delegates work appropriately. No UNC paths are ever used with NuGet/dotnet.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release
.PARAMETER SkipTests
    Skip running tests
.PARAMETER PackOnly
    Only create NuGet packages
.PARAMETER Clean
    Clean before building
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Configuration Debug
    .\build.ps1 -PackOnly
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$PackOnly,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

# Helper functions
function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Blue
    Write-Host "  $Text" -ForegroundColor White
    Write-Host ("=" * 60) -ForegroundColor Blue
}

function Write-Success { Write-Host "✓ $args" -ForegroundColor Green }
function Write-Info { Write-Host "→ $args" -ForegroundColor Cyan }
function Write-Warning { Write-Host "⚠ $args" -ForegroundColor Yellow }
function Write-Error { Write-Host "✗ $args" -ForegroundColor Red }

try {
    Write-Header "FastFSM Universal Build Script"
    
    # Determine where we are and what strategy to use
    $currentPath = $PWD.Path
    $isWslPath = $currentPath -like "*\\wsl*" -or $currentPath -like "*wsl.localhost*"
    $localFeed = "$env:LOCALAPPDATA\FastFsm\nuget"
    
    Write-Info "Current location: $currentPath"
    Write-Info "Is WSL path: $isWslPath"
    Write-Info "Local feed: $localFeed"
    
    # Ensure local feed directory exists
    New-Item -ItemType Directory -Force -Path $localFeed | Out-Null
    
    # Strategy 1: If we're in WSL path, delegate to WSL
    if ($isWslPath) {
        Write-Header "Strategy: Delegate to WSL"
        Write-Info "Detected WSL path - will use WSL for building"
        
        # Extract WSL path components
        $wslDistro = "Ubuntu-24.04"  # Default, could be made configurable
        if ($currentPath -match '\\\\wsl[^\\]*\\([^\\]+)\\(.+)$') {
            $wslDistro = $matches[1]
            $wslPath = "/" + ($matches[2] -replace '\\', '/')
        } elseif ($currentPath -match 'wsl\.localhost\\([^\\]+)\\(.+)$') {
            $wslDistro = $matches[1]
            $wslPath = "/" + ($matches[2] -replace '\\', '/')
        } else {
            throw "Cannot parse WSL path: $currentPath"
        }
        
        Write-Info "WSL distro: $wslDistro"
        Write-Info "WSL path: $wslPath"
        
        # Build command for WSL
        $wslFeed = "/mnt/c" + $localFeed.Substring(2).Replace('\', '/')
        $wslArgs = @()
        if ($Configuration -eq "Debug") { $wslArgs += "-c Debug" }
        if ($SkipTests) { $wslArgs += "--skip-tests" }
        if ($PackOnly) { $wslArgs += "--pack-only" }
        if ($Clean) { $wslArgs += "--clean" }
        $wslArgs += "--out '$wslFeed'"
        
        $wslCmd = "cd '$wslPath' && ./build-and-test.sh $($wslArgs -join ' ')"
        Write-Info "Executing in WSL: $wslCmd"
        
        # Execute in WSL
        $result = wsl.exe -d $wslDistro bash -lc $wslCmd
        
        if ($LASTEXITCODE -ne 0) {
            throw "WSL build failed"
        }
        
        Write-Success "WSL build completed"
        
    } 
    # Strategy 2: We're on Windows proper - build locally
    else {
        Write-Header "Strategy: Local Windows Build"
        Write-Info "Running from Windows path - building locally"
        
        # Check if we have the solution file
        if (-not (Test-Path ".\FastFsm.Net.slnx")) {
            throw "FastFsm.Net.slnx not found. Please run from repository root."
        }
        
        # Read version for package naming
        $versionJson = Get-Content -Raw ".\version.json" | ConvertFrom-Json
        $version = "$($versionJson.version).$($versionJson.buildNumber)"
        if ($versionJson.suffix) { $version += "-$($versionJson.suffix)" }
        
        Write-Info "Version: $version"
        
        # Clean if requested
        if ($Clean) {
            Write-Info "Cleaning..."
            dotnet clean -c $Configuration -v minimal
            Remove-Item "$localFeed\*.nupkg" -ErrorAction SilentlyContinue
        }
        
        # Create temporary nuget.config with local feed
        $tempConfig = Join-Path $env:TEMP "fastfsm.nuget.temp.config"
        @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="LocalFastFsm" value="$localFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -Encoding UTF8 $tempConfig
        
        Write-Info "Created temp config: $tempConfig"
        
        # Restore
        Write-Header "Restoring packages"
        dotnet restore ".\FastFsm.Net.slnx" `
            --configfile $tempConfig `
            --force `
            --no-cache
        
        if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
        
        # Build
        if (-not $PackOnly) {
            Write-Header "Building solution"
            dotnet build ".\FastFsm.Net.slnx" `
                --configfile $tempConfig `
                --no-restore `
                -c $Configuration `
                -v minimal
            
            if ($LASTEXITCODE -ne 0) { throw "Build failed" }
        }
        
        # Pack
        Write-Header "Creating NuGet packages"
        
        $projects = @(
            ".\FastFsm\FastFsm.csproj",
            ".\FastFsm.Logging\FastFsm.Logging.csproj",
            ".\FastFsm.DependencyInjection\FastFsm.DependencyInjection.csproj"
        )
        
        foreach ($project in $projects) {
            if (Test-Path $project) {
                Write-Info "Packing $(Split-Path -Leaf $project)..."
                dotnet pack $project `
                    -c $Configuration `
                    -p:PackageVersion=$version `
                    --no-restore `
                    $(if (-not $PackOnly) { "--no-build" }) `
                    --output $localFeed `
                    -v minimal
                
                if ($LASTEXITCODE -ne 0) { 
                    Write-Warning "Failed to pack $project"
                }
            }
        }
        
        # Test
        if (-not $SkipTests -and -not $PackOnly) {
            Write-Header "Running tests"
            dotnet test ".\FastFsm.Net.slnx" `
                --configfile $tempConfig `
                --no-build `
                --no-restore `
                -c $Configuration `
                -v minimal
            
            if ($LASTEXITCODE -ne 0) { 
                Write-Warning "Some tests failed"
            }
        }
        
        # Cleanup
        Remove-Item $tempConfig -ErrorAction SilentlyContinue
    }
    
    # Show summary
    Write-Header "Build Complete"
    Write-Success "Configuration: $Configuration"
    Write-Success "Local feed: $localFeed"
    
    # Show latest packages
    $packages = Get-ChildItem "$localFeed\*.nupkg" -ErrorAction SilentlyContinue | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 3
    
    if ($packages) {
        Write-Info "Latest packages:"
        $packages | ForEach-Object {
            Write-Host "  - $($_.Name)" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Success "Build completed successfully!"
    
} catch {
    Write-Host ""
    Write-Error "Build failed: $_"
    exit 1
}