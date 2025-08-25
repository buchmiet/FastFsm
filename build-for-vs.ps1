#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Quick build script for Visual Studio 2022 integration
.DESCRIPTION
    This script is optimized for use within Visual Studio 2022.
    It builds the NuGet package and prepares tests for debugging in VS.
.PARAMETER QuickMode
    Skip tests and just build the package (faster for iterative development)
.PARAMETER WslRoot
    WSL root path to FastFsm repository
.PARAMETER WinFeed
    Windows local feed path for NuGet packages
#>

param(
    [switch]$QuickMode,
    [string]$WslRoot = "\\wsl.localhost\Ubuntu-24.04\home\lukasz\FastFsm",
    [string]$WinFeed = "$env:LOCALAPPDATA\FastFsm\nuget"
)

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "  FastFSM Quick Build for Visual Studio 2022" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

try {
    # Ensure Windows feed directory exists
    New-Item -ItemType Directory -Force -Path $WinFeed | Out-Null
    
    # Set NuGet cache to Windows location
    if (-not $env:NUGET_PACKAGES) {
        $env:NUGET_PACKAGES = Join-Path $env:LOCALAPPDATA 'NuGet\Packages'
    }
    
    # Read current version using safe JSON parsing
    $versionPath = Join-Path $WslRoot "version.json"
    $ver = Get-Content -Raw -LiteralPath $versionPath | ConvertFrom-Json
    
    # Ensure buildNumber exists
    if ($null -eq $ver.buildNumber) { 
        $ver | Add-Member -NotePropertyName buildNumber -NotePropertyValue 0 
    }
    
    # Increment build number
    $ver.buildNumber = [int]$ver.buildNumber + 1
    
    # Construct full version
    $suffixPart = if ($ver.suffix -and $ver.suffix -ne 'null' -and $ver.suffix.Trim() -ne '') { 
        "-$($ver.suffix)" 
    } else { 
        "" 
    }
    $fullVersion = "{0}.{1}{2}" -f $ver.version, $ver.buildNumber, $suffixPart
    
    # Save version back
    $ver | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath $versionPath
    
    Write-Host "Building version: " -NoNewline
    Write-Host $fullVersion -ForegroundColor Green
    
    # Create WSL nuget directory if needed
    $wslNuget = Join-Path $WslRoot 'nuget'
    if (-not (Test-Path $wslNuget)) { 
        New-Item -ItemType Directory -Path $wslNuget | Out-Null 
    }
    
    # Generate temporary NuGet config with Windows feed
    $cfg = Join-Path $env:TEMP 'fastfsm.nuget.windows.config'
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="FastFsmLocalWin" value="$WinFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -Encoding UTF8 $cfg
    
    Write-Host "Created temporary NuGet config: $cfg" -ForegroundColor Green
    
    # Clear NuGet cache
    Write-Host "Clearing NuGet cache..." -ForegroundColor Cyan
    dotnet nuget locals temp --clear | Out-Null
    
    # RESTORE FIRST with config file
    Write-Host "Restoring solution..." -ForegroundColor Cyan
    dotnet restore "$WslRoot\FastFsm.Net.slnx" `
        --configfile "$cfg" `
        --force `
        --no-cache
    
    if ($LASTEXITCODE -ne 0) {
        throw "Solution restore failed"
    }
    
    # BUILD packages with --no-restore (to Windows feed)
    Write-Host "Building packages..." -ForegroundColor Cyan
    
    # FastFsm.Net
    dotnet pack "$WslRoot\FastFsm\FastFsm.csproj" `
        -c Release `
        -p:PackageVersion=$fullVersion `
        --no-restore `
        -o "$WinFeed" `
        -v quiet
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build FastFsm.Net package"
    }
    Write-Host "✓ Created FastFsm.Net.$fullVersion.nupkg" -ForegroundColor Green
    
    # FastFsm.Net.Logging (if exists)
    if (Test-Path "$WslRoot\FastFsm.Logging\FastFsm.Logging.csproj") {
        dotnet pack "$WslRoot\FastFsm.Logging\FastFsm.Logging.csproj" `
            -c Release `
            -p:PackageVersion=$fullVersion `
            --no-restore `
            -o "$WinFeed" `
            -v quiet
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Created FastFsm.Net.Logging.$fullVersion.nupkg" -ForegroundColor Green
        }
    }
    
    # FastFsm.Net.DependencyInjection (if exists)
    if (Test-Path "$WslRoot\FastFsm.DependencyInjection\FastFsm.DependencyInjection.csproj") {
        dotnet pack "$WslRoot\FastFsm.DependencyInjection\FastFsm.DependencyInjection.csproj" `
            -c Release `
            -p:PackageVersion=$fullVersion `
            --no-restore `
            -o "$WinFeed" `
            -v quiet
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Created FastFsm.Net.DependencyInjection.$fullVersion.nupkg" -ForegroundColor Green
        }
    }
    
    # Also copy to WSL nuget folder for consistency
    Write-Host "Mirroring packages to WSL feed..." -ForegroundColor Cyan
    Copy-Item "$WinFeed\*.nupkg" -Destination $wslNuget -Force
    
    # Update test projects
    Write-Host "Updating test projects..." -ForegroundColor Cyan
    
    $testProjects = @(
        "$WslRoot\FastFsm.Tests\FastFsm.Tests.csproj",
        "$WslRoot\FastFsm.Async.Tests\FastFsm.Async.Tests.csproj",
        "$WslRoot\FastFsm.Logging.Tests\FastFsm.Logging.Tests.csproj",
        "$WslRoot\FastFsm.DependencyInjection.Tests\FastFsm.DependencyInjection.Tests.csproj"
    )
    
    foreach ($project in $testProjects) {
        if (Test-Path $project) {
            $content = Get-Content $project -Raw
            
            # Update package references
            $content = $content -replace '<PackageReference Include="FastFsm\.Net" Version="[^"]*"', `
                                         "<PackageReference Include=`"FastFsm.Net`" Version=`"$fullVersion`""
            $content = $content -replace '<PackageReference Include="FastFsm\.Net\.Logging" Version="[^"]*"', `
                                         "<PackageReference Include=`"FastFsm.Net.Logging`" Version=`"$fullVersion`""
            $content = $content -replace '<PackageReference Include="FastFsm\.Net\.DependencyInjection" Version="[^"]*"', `
                                         "<PackageReference Include=`"FastFsm.Net.DependencyInjection`" Version=`"$fullVersion`""
            
            $content | Set-Content $project
        }
    }
    
    # Final restore with updated packages
    Write-Host "Final restore..." -ForegroundColor Cyan
    dotnet restore "$WslRoot\FastFsm.Net.slnx" --configfile "$cfg" --force
    
    if ($LASTEXITCODE -ne 0) {
        throw "Final restore failed"
    }
    
    if (-not $QuickMode) {
        Write-Host "Building solution..." -ForegroundColor Cyan
        dotnet build "$WslRoot\FastFsm.Net.slnx" `
            --configfile "$cfg" `
            --no-restore `
            -c Debug `
            -v quiet
        
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed"
        }
        
        Write-Host "Running tests..." -ForegroundColor Cyan
        dotnet test "$WslRoot\FastFsm.Net.slnx" `
            --configfile "$cfg" `
            --no-build `
            --no-restore `
            -v quiet
        
        Write-Host "✓ Test projects ready" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "✓ Build complete!" -ForegroundColor Green
    Write-Host "  Package: FastFsm.Net.$fullVersion" -ForegroundColor White
    Write-Host "  Windows Feed: $WinFeed" -ForegroundColor White
    Write-Host "  You can now run/debug tests in Visual Studio" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
    
} catch {
    Write-Host "Build failed: $_" -ForegroundColor Red
    exit 1
}