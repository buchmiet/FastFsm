#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build FastFSM NuGet package and run tests
.DESCRIPTION
    This script builds the FastFSM NuGet package with proper versioning,
    updates test projects to use the new package, and runs all tests.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release
.PARAMETER SkipTests
    Skip running tests after building
.PARAMETER Clean
    Clean all build artifacts before building
.PARAMETER Version
    Override version from version.json
.PARAMETER NoIncrement
    Don't auto-increment build number
.EXAMPLE
    .\build-and-test.ps1
    .\build-and-test.ps1 -Configuration Debug
    .\build-and-test.ps1 -SkipTests
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [switch]$SkipTests,
    
    [switch]$Clean,
    
    [string]$Version = $null,
    
    [switch]$NoIncrement
)

$ErrorActionPreference = "Stop"

# Color output helpers
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }
function Write-Header { 
    Write-Host ""
    Write-Host "═" -NoNewline -ForegroundColor Blue
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "  $args" -ForegroundColor White
    Write-Host "═" -NoNewline -ForegroundColor Blue
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Blue
}

try {
    Write-Header "FastFSM Build and Test Script"
    
    # DIAGNOSTYKA
    Write-Warning "=== DIAGNOSTICS START ==="
    Write-Info "Current PWD.Path: $($PWD.Path)"
    Write-Info "Current PWD.Provider: $($PWD.Provider)"
    Write-Info "Current Location: $(Get-Location)"
    
    # Handle WSL paths - convert to drive letter FIRST
    $needPopd = $false
    $needNetUse = $false
    $mappedDrive = $null
    
    if ($PWD.Path -like '*\\wsl*' -or $PWD.Provider.ToString() -like '*FileSystem*\\wsl*') {
        Write-Info "Detected WSL path, converting to drive letter..."
        Write-Info "Original path: $($PWD.Path)"
        
        # Extract the clean path without provider prefix
        $cleanPath = $PWD.Path
        if ($cleanPath -match 'FileSystem::(.+)$') {
            $cleanPath = $matches[1]
            Write-Info "Cleaned path: $cleanPath"
        }
        
        # Convert to \\wsl.localhost\ format if needed
        $wslPath = $cleanPath
        if ($wslPath -like '\\wsl$\*') {
            $wslPath = $wslPath -replace '^\\\\wsl\$\\', '\\wsl.localhost\'
            Write-Info "Converted to: $wslPath"
        }
        
        # Try pushd first (works on some Windows versions)
        Write-Info "Attempting pushd to: $wslPath"
        $pushdOutput = pushd $wslPath 2>&1
        
        # Check if we got a drive letter
        $currentPath = (Get-Location).ProviderPath
        if (-not $currentPath) {
            $currentPath = (Get-Location).Path
        }
        
        Write-Info "Current path after pushd: $currentPath"
        
        if ($currentPath -match '^([A-Z]):') {
            # Success! We have a drive letter
            $needPopd = $true
            Write-Success "Successfully mapped to drive: $($matches[1]):"
        } else {
            # pushd didn't give us a drive letter, try net use
            Write-Warning "pushd didn't create drive mapping, trying net use..."
            
            # Find a free drive letter
            $freeDrive = $null
            foreach ($letter in 'ZYXWVUTSRQPONMLKJIHGFED'.ToCharArray()) {
                $testDrive = "${letter}:"
                if (-not (Test-Path "${testDrive}\")) {
                    $freeDrive = $testDrive
                    Write-Info "Found free drive letter: $freeDrive"
                    break
                }
            }
            
            if ($freeDrive) {
                Write-Info "Executing: net use $freeDrive $wslPath"
                $netUseResult = & cmd /c "net use $freeDrive `"$wslPath`" 2>&1"
                
                if ($LASTEXITCODE -eq 0) {
                    Set-Location $freeDrive
                    $mappedDrive = $freeDrive
                    $needNetUse = $true
                    Write-Success "Successfully mapped $wslPath to $freeDrive"
                } else {
                    Write-Error "Failed to map drive: $netUseResult"
                    throw "Cannot map WSL path to drive letter"
                }
            } else {
                throw "No free drive letters available"
            }
        }
    } else {
        Write-Info "Not a WSL path, using current directory"
    }
    
    Write-Warning "=== DIAGNOSTICS END ==="
    
    # Set paths AFTER potential pushd
    $RepoRoot = (Get-Location).Path
    Write-Info "RepoRoot set to: $RepoRoot"
    
    $NugetFolder = Join-Path $RepoRoot 'nuget'
    Write-Info "NugetFolder set to: $NugetFolder"
    New-Item -ItemType Directory -Force -Path $NugetFolder | Out-Null
    
    # Set NuGet cache to Windows location
    if (-not $env:NUGET_PACKAGES) {
        $env:NUGET_PACKAGES = Join-Path $env:LOCALAPPDATA 'NuGet\Packages'
        Write-Info "Set NUGET_PACKAGES to: $env:NUGET_PACKAGES"
    }
    
    # Read and update version using JSON cmdlets (no regex!)
    Write-Info "Reading version configuration..."
    $versionPath = Join-Path $RepoRoot 'version.json'
    $ver = Get-Content -Raw -LiteralPath $versionPath | ConvertFrom-Json
    
    # Ensure buildNumber exists
    if ($null -eq $ver.buildNumber) { 
        $ver | Add-Member -NotePropertyName buildNumber -NotePropertyValue 0 
    }
    
    # Override version if provided
    if ($Version) {
        $ver.version = $Version
    }
    
    # Increment build number if enabled
    if (-not $NoIncrement -and $ver.autoIncrement -eq $true) { 
        $ver.buildNumber = [int]$ver.buildNumber + 1 
        Write-Info "Incremented build number to $($ver.buildNumber)"
    }
    
    # Construct full version
    $suffixPart = if ($ver.suffix -and $ver.suffix -ne 'null' -and $ver.suffix.Trim() -ne '') { 
        "-$($ver.suffix)" 
    } else { 
        "" 
    }
    $FULL_VERSION = "{0}.{1}{2}" -f $ver.version, $ver.buildNumber, $suffixPart
    
    # Save version back
    $ver | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath $versionPath
    Write-Success "Building version: $FULL_VERSION"
    
    # Generate temporary nuget.config with drive letter paths
    $TempConfig = Join-Path $RepoRoot '.nuget.windows.config'
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="LocalFastFsm" value="$NugetFolder" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -Encoding UTF8 $TempConfig
    
    Write-Success "Created temporary NuGet config: $TempConfig"
    
    # Clean if requested
    if ($Clean) {
        Write-Header "Cleaning Solution"
        dotnet clean "$RepoRoot\FastFsm.Net.slnx" -c $Configuration -v minimal
        if ($LASTEXITCODE -ne 0) { throw "Clean failed" }
        
        # Remove old packages
        Write-Info "Removing old local packages..."
        Remove-Item "$NugetFolder\FastFsm.Net.*.nupkg" -ErrorAction SilentlyContinue
        Remove-Item "$NugetFolder\FastFsm.Net.Logging.*.nupkg" -ErrorAction SilentlyContinue
    }
    
    # Clear NuGet caches
    Write-Header "Clearing NuGet Caches"
    Write-Info "Clearing local NuGet cache..."
    dotnet nuget locals temp --clear | Out-Null
    
    # RESTORE FIRST with config file
    Write-Header "Restoring Packages"
    Write-Info "Restoring solution with config: $TempConfig"
    dotnet restore "$RepoRoot\FastFsm.Net.slnx" `
        --configfile "$TempConfig" `
        --force `
        --no-cache
    
    if ($LASTEXITCODE -ne 0) { 
        throw "Solution restore failed" 
    }
    Write-Success "✓ Restore completed"
    
    # BUILD FastFsm package (with --no-restore)
    Write-Header "Building FastFsm NuGet Packages"
    Write-Info "Building FastFsm.Net package..."
    
    dotnet pack "$RepoRoot\FastFsm\FastFsm.csproj" `
        -c $Configuration `
        -p:PackageVersion="$FULL_VERSION" `
        --no-restore `
        --output "$NugetFolder" `
        -v minimal
    
    if ($LASTEXITCODE -ne 0) { 
        throw "Failed to build FastFsm.Net package" 
    }
    Write-Success "✓ Created FastFsm.Net.$FULL_VERSION.nupkg"
    
    # Build FastFsm.Logging package if it exists
    if (Test-Path "$RepoRoot\FastFsm.Logging\FastFsm.Logging.csproj") {
        Write-Info "Building FastFsm.Net.Logging package..."
        
        dotnet pack "$RepoRoot\FastFsm.Logging\FastFsm.Logging.csproj" `
            -c $Configuration `
            -p:PackageVersion="$FULL_VERSION" `
            --no-restore `
            --output "$NugetFolder" `
            -v minimal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✓ Created FastFsm.Net.Logging.$FULL_VERSION.nupkg"
        } else {
            Write-Warning "Failed to build FastFsm.Net.Logging package"
        }
    }
    
    # Build FastFsm.DependencyInjection package if it exists
    if (Test-Path "$RepoRoot\FastFsm.DependencyInjection\FastFsm.DependencyInjection.csproj") {
        Write-Info "Building FastFsm.Net.DependencyInjection package..."
        
        dotnet pack "$RepoRoot\FastFsm.DependencyInjection\FastFsm.DependencyInjection.csproj" `
            -c $Configuration `
            -p:PackageVersion="$FULL_VERSION" `
            --no-restore `
            --output "$NugetFolder" `
            -v minimal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✓ Created FastFsm.Net.DependencyInjection.$FULL_VERSION.nupkg"
        }
    }
    
    # Update test projects to use new package version
    Write-Header "Updating Test Projects"
    
    $testProjects = @(
        "$RepoRoot\FastFsm.Tests\FastFsm.Tests.csproj",
        "$RepoRoot\FastFsm.Async.Tests\FastFsm.Async.Tests.csproj",
        "$RepoRoot\FastFsm.Logging.Tests\FastFsm.Logging.Tests.csproj",
        "$RepoRoot\FastFsm.DependencyInjection.Tests\FastFsm.DependencyInjection.Tests.csproj"
    )
    
    foreach ($testProject in $testProjects) {
        if (Test-Path $testProject) {
            $projectName = Split-Path $testProject -Leaf
            Write-Info "Updating $projectName..."
            
            $testContent = Get-Content $testProject -Raw
            
            # Update FastFsm.Net package reference
            $testContent = $testContent -replace `
                '<PackageReference\s+Include="FastFsm\.Net"\s+Version="[^"]*"', `
                "<PackageReference Include=`"FastFsm.Net`" Version=`"$FULL_VERSION`""
            
            # Update FastFsm.Net.Logging package reference if present
            $testContent = $testContent -replace `
                '<PackageReference\s+Include="FastFsm\.Net\.Logging"\s+Version="[^"]*"', `
                "<PackageReference Include=`"FastFsm.Net.Logging`" Version=`"$FULL_VERSION`""
            
            # Update FastFsm.Net.DependencyInjection package reference if present
            $testContent = $testContent -replace `
                '<PackageReference\s+Include="FastFsm\.Net\.DependencyInjection"\s+Version="[^"]*"', `
                "<PackageReference Include=`"FastFsm.Net.DependencyInjection`" Version=`"$FULL_VERSION`""
            
            $testContent | Set-Content $testProject
        }
    }
    
    # Restore again with updated packages
    Write-Header "Final Restore with Updated Packages"
    dotnet restore "$RepoRoot\FastFsm.Net.slnx" `
        --configfile "$TempConfig" `
        --force
    
    if ($LASTEXITCODE -ne 0) { 
        throw "Final restore failed" 
    }
    
    # Build solution
    Write-Header "Building Solution"
    dotnet build "$RepoRoot\FastFsm.Net.slnx" `
        --configfile "$TempConfig" `
        --no-restore `
        -c $Configuration `
        -v minimal
    
    if ($LASTEXITCODE -ne 0) { 
        throw "Build failed" 
    }
    Write-Success "✓ Build completed"
    
    # Run tests if not skipped
    if (-not $SkipTests) {
        Write-Header "Running Tests"
        
        $testResults = @()
        $failedTests = @()
        
        foreach ($testProject in $testProjects) {
            if (Test-Path $testProject) {
                $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)
                Write-Info "Running $projectName..."
                
                $output = dotnet test $testProject `
                    --configfile "$TempConfig" `
                    -c $Configuration `
                    --no-build `
                    --no-restore `
                    --verbosity normal `
                    2>&1
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "✓ $projectName passed"
                    $testResults += "✓ $projectName"
                } else {
                    Write-Error "✗ $projectName failed"
                    $failedTests += $projectName
                    $testResults += "✗ $projectName"
                    
                    # Show test output on failure
                    Write-Warning "Test output:"
                    $output | ForEach-Object { Write-Host "  $_" }
                }
            }
        }
        
        # Summary
        Write-Header "Test Summary"
        $testResults | ForEach-Object { Write-Host $_ }
        
        if ($failedTests.Count -gt 0) {
            throw "Tests failed: $($failedTests -join ', ')"
        } else {
            Write-Success "All tests passed!"
        }
    }
    
    # Final summary
    Write-Header "Build Complete"
    Write-Success "Package version: $FULL_VERSION"
    Write-Success "Configuration: $Configuration"
    Write-Success "Package location: $NugetFolder"
    
    if (Test-Path "$NugetFolder\FastFsm.Net.$FULL_VERSION.nupkg") {
        $packageInfo = Get-Item "$NugetFolder\FastFsm.Net.$FULL_VERSION.nupkg"
        Write-Info "Package size: $([math]::Round($packageInfo.Length / 1KB, 2)) KB"
    }
    
    Write-Host ""
    Write-Success "Build completed successfully!"
    
} catch {
    Write-Error "Build failed: $_"
    exit 1
} finally {
    # Clean up - remove drive mapping or popd
    if ($needNetUse -and $mappedDrive) {
        Write-Info "Removing drive mapping: $mappedDrive"
        & cmd /c "net use $mappedDrive /delete /y" 2>$null
    } elseif ($needPopd) {
        popd
        Write-Info "Restored original directory"
    }
}
