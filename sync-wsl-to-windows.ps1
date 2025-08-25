#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sync FastFSM repository from WSL to Windows (optional tool)
.DESCRIPTION
    Creates a Windows-native copy of the repository for those who prefer
    to work entirely in Windows without any WSL paths.
.PARAMETER WslDistro
    WSL distribution name
.PARAMETER WslRepo
    Path to repository in WSL
.PARAMETER WinRepo
    Target path on Windows
.PARAMETER Watch
    Keep watching for changes
.EXAMPLE
    .\sync-wsl-to-windows.ps1
    .\sync-wsl-to-windows.ps1 -WinRepo "D:\Projects\FastFsm"
#>

param(
    [string]$WslDistro = "Ubuntu-24.04",
    [string]$WslRepo = "/home/lukasz/FastFsm",
    [string]$WinRepo = "$env:USERPROFILE\source\repos\FastFsm.windows",
    [switch]$Watch
)

$ErrorActionPreference = "Stop"

Write-Host "FastFSM WSL to Windows Sync" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

# Check if WSL is available
$wslDistros = wsl.exe -l -q 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "WSL is not installed or not available" -ForegroundColor Red
    exit 1
}

if ($wslDistros -notcontains $WslDistro) {
    Write-Host "WSL distro '$WslDistro' not found" -ForegroundColor Red
    Write-Host "Available distros:" -ForegroundColor Yellow
    $wslDistros | ForEach-Object { Write-Host "  - $_" }
    exit 1
}

# Create target directory
New-Item -ItemType Directory -Force -Path $WinRepo | Out-Null

Write-Host "Source: WSL:$WslDistro:$WslRepo" -ForegroundColor Cyan
Write-Host "Target: $WinRepo" -ForegroundColor Cyan
Write-Host ""

# Function to perform sync
function Sync-Repository {
    Write-Host "$(Get-Date -Format 'HH:mm:ss') Syncing..." -ForegroundColor Gray
    
    # Convert Windows path to WSL format
    $winRepoWsl = "/mnt/" + $WinRepo.Substring(0,1).ToLower() + $WinRepo.Substring(2).Replace('\', '/')
    
    # Build rsync command
    $rsyncCmd = @"
if ! command -v rsync &> /dev/null; then
    echo "Installing rsync..."
    sudo apt-get update && sudo apt-get install -y rsync
fi

rsync -av --delete \
    --exclude='.git' \
    --exclude='bin/' \
    --exclude='obj/' \
    --exclude='.vs/' \
    --exclude='*.user' \
    --exclude='packages/' \
    --exclude='node_modules/' \
    --exclude='.nuget.windows.config' \
    '$WslRepo/' '$winRepoWsl/'
"@
    
    # Execute rsync in WSL
    $output = wsl.exe -d $WslDistro bash -c $rsyncCmd 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        # Count synced files
        $fileCount = ($output | Where-Object { $_ -match '^[^/]+/$' }).Count
        Write-Host "✓ Synced successfully" -ForegroundColor Green
        if ($fileCount -gt 0) {
            Write-Host "  Updated $fileCount items" -ForegroundColor Gray
        }
    } else {
        Write-Host "✗ Sync failed" -ForegroundColor Red
        Write-Host $output
        return $false
    }
    
    return $true
}

# Perform initial sync
if (-not (Sync-Repository)) {
    exit 1
}

# Watch mode
if ($Watch) {
    Write-Host ""
    Write-Host "Watching for changes (Ctrl+C to stop)..." -ForegroundColor Yellow
    Write-Host ""
    
    # Use WSL inotify to watch for changes
    $watchCmd = @"
while true; do
    inotifywait -r -e modify,create,delete,move \
        --exclude '(\.git|bin|obj|\.vs)' \
        '$WslRepo' 2>/dev/null
    echo "CHANGED"
done
"@
    
    # Start watching in background
    $watchProcess = Start-Process -NoNewWindow -PassThru -FilePath "wsl.exe" `
        -ArgumentList "-d", $WslDistro, "bash", "-c", $watchCmd `
        -RedirectStandardOutput "$env:TEMP\wsl-watch.txt"
    
    try {
        while ($true) {
            Start-Sleep -Seconds 2
            if (Test-Path "$env:TEMP\wsl-watch.txt") {
                $content = Get-Content "$env:TEMP\wsl-watch.txt" -Tail 1 -ErrorAction SilentlyContinue
                if ($content -eq "CHANGED") {
                    Clear-Content "$env:TEMP\wsl-watch.txt"
                    Sync-Repository
                }
            }
        }
    } finally {
        $watchProcess | Stop-Process -Force -ErrorAction SilentlyContinue
        Remove-Item "$env:TEMP\wsl-watch.txt" -ErrorAction SilentlyContinue
    }
} else {
    Write-Host ""
    Write-Host "Repository synced to: $WinRepo" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now:" -ForegroundColor Cyan
    Write-Host "  1. Open $WinRepo in Visual Studio" -ForegroundColor Gray
    Write-Host "  2. Run .\build.ps1 from that location" -ForegroundColor Gray
    Write-Host "  3. Use -Watch flag to keep syncing changes" -ForegroundColor Gray
}