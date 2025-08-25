#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Simple build script for Visual Studio 2022 - no UNC paths
.DESCRIPTION
    Uses WSL to pack packages, then builds/tests in Windows using local feed.
    No \\wsl$ paths involved!
.PARAMETER WslDistro
    WSL distribution name
.PARAMETER WslRepo
    Path to repository in WSL
.PARAMETER WinFeed
    Windows local feed path for NuGet packages
.PARAMETER SkipPack
    Skip WSL packing step (use existing packages)
.PARAMETER QuickMode
    Skip tests
#>

param(
    [string]$WslDistro = "Ubuntu-24.04",
    [string]$WslRepo = "/home/lukasz/FastFsm",
    [string]$WinFeed = "$env:LOCALAPPDATA\FastFsm\nuget",
    [switch]$SkipPack,
    [switch]$QuickMode
)

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "  FastFSM Build for Visual Studio 2022 (Simple)" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

try {
    # Ensure Windows feed directory exists
    New-Item -ItemType Directory -Force -Path $WinFeed | Out-Null
    Write-Host "Windows feed: $WinFeed" -ForegroundColor Cyan
    
    if (-not $SkipPack) {
        # Step 1: Use WSL to pack packages to Windows feed
        Write-Host ""
        Write-Host "Step 1: Packing in WSL..." -ForegroundColor Green
        
        # Convert Windows path to WSL path
        $wslFeed = "/mnt/c" + $WinFeed.Substring(2).Replace('\', '/')
        Write-Host "WSL output path: $wslFeed" -ForegroundColor Cyan
        
        # Build command for WSL
        $wslCmd = "cd '$WslRepo' && ./build-and-test.sh --pack-only --out '$wslFeed'"
        Write-Host "Executing in WSL: $WslDistro" -ForegroundColor Cyan
        
        # Execute in WSL
        $wslOutput = wsl.exe -d $WslDistro bash -lc $wslCmd 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host $wslOutput -ForegroundColor Red
            throw "WSL packing failed"
        }
        
        # Show last few lines of output
        $wslOutput -split "`n" | Select-Object -Last 5 | ForEach-Object { 
            Write-Host $_ -ForegroundColor Gray 
        }
        
        Write-Host "✓ Packing completed in WSL" -ForegroundColor Green
    } else {
        Write-Host "Skipping WSL pack step (using existing packages)" -ForegroundColor Yellow
    }
    
    # Step 2: Create temporary NuGet config with local feed
    Write-Host ""
    Write-Host "Step 2: Creating NuGet config..." -ForegroundColor Green
    
    $cfg = Join-Path $env:TEMP 'fastfsm.nuget.windows.config'
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="FastFsmLocal" value="$WinFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -Encoding UTF8 $cfg
    
    Write-Host "Config created: $cfg" -ForegroundColor Cyan
    
    # Step 3: Restore in Windows (from current directory)
    Write-Host ""
    Write-Host "Step 3: Restoring packages..." -ForegroundColor Green
    
    # Assume we're in the repo root (or provide explicit path)
    if (Test-Path ".\FastFsm.Net.slnx") {
        $solutionPath = ".\FastFsm.Net.slnx"
    } else {
        Write-Host "Solution not found in current directory." -ForegroundColor Yellow
        Write-Host "Please run this script from the repository root." -ForegroundColor Yellow
        throw "FastFsm.Net.slnx not found"
    }
    
    dotnet restore $solutionPath --configfile "$cfg" --force --no-cache
    
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed"
    }
    Write-Host "✓ Restore completed" -ForegroundColor Green
    
    # Step 4: Build solution
    Write-Host ""
    Write-Host "Step 4: Building solution..." -ForegroundColor Green
    
    dotnet build $solutionPath `
        --configfile "$cfg" `
        --no-restore `
        -c Debug `
        -v quiet
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "✓ Build completed" -ForegroundColor Green
    
    # Step 5: Run tests (unless QuickMode)
    if (-not $QuickMode) {
        Write-Host ""
        Write-Host "Step 5: Running tests..." -ForegroundColor Green
        
        dotnet test $solutionPath `
            --configfile "$cfg" `
            --no-build `
            --no-restore `
            -v quiet
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ All tests passed" -ForegroundColor Green
        } else {
            Write-Host "⚠ Some tests failed" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Skipping tests (QuickMode)" -ForegroundColor Yellow
    }
    
    # Summary
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "✓ Build complete!" -ForegroundColor Green
    Write-Host "  Local feed: $WinFeed" -ForegroundColor White
    
    # Show latest packages
    $latestPackages = Get-ChildItem "$WinFeed\*.nupkg" | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 3
    
    if ($latestPackages) {
        Write-Host "  Latest packages:" -ForegroundColor White
        $latestPackages | ForEach-Object {
            Write-Host "    - $($_.Name)" -ForegroundColor Gray
        }
    }
    
    Write-Host "  You can now debug tests in Visual Studio" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
    
} catch {
    Write-Host ""
    Write-Host "Build failed: $_" -ForegroundColor Red
    exit 1
}