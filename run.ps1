# run: pwsh -File .\diag-pack.ps1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "=== FastFSM DIAG + PACK ===" -ForegroundColor Cyan

# --- CONFIG ---
$RepoRoot = Get-Location
$NugetDir = Join-Path $RepoRoot 'nuget'
$CoreProj = Join-Path $RepoRoot 'FastFsm/FastFsm.csproj'
$LogProj  = Join-Path $RepoRoot 'FastFsm.Logging/FastFsm.Logging.csproj'
$Props    = Join-Path $RepoRoot 'FastFsm.Logging/build/FastFsm.Net.Logging.props'
$CoreVersion = "0.6.9-local.diag.$(Get-Date -Format yyyyMMddHHmmss)"
$LogVersion  = $CoreVersion

# --- PRECHECKS ---
Write-Host "`n[Prechecks]" -ForegroundColor Yellow
foreach($p in @($CoreProj,$LogProj)) {
  if(-not (Test-Path $p)) { throw "Missing file: $p" }
}
New-Item -ItemType Directory -Force -Path $NugetDir | Out-Null

# --- CLEAN ---
Write-Host "`n[Clean caches + outputs]" -ForegroundColor Yellow
try {
  dotnet nuget locals all --clear | Out-Host
} catch { Write-Warning "Clearing nuget locals failed (continuing): $($_.Exception.Message)" }

Get-ChildItem -Recurse -Directory -Force -Include bin,obj |
  ForEach-Object { try { Remove-Item -Recurse -Force $_.FullName } catch {} }

Remove-Item "$NugetDir\*" -Force -ErrorAction SilentlyContinue

# --- SHOW SOURCES ---
Write-Host "`n[NuGet sources]" -ForegroundColor Yellow
dotnet nuget list source | Out-Host

# --- BUILD SOLUTION (optional but helpful) ---
Write-Host "`n[Restore + Build solution]" -ForegroundColor Yellow
dotnet restore | Out-Host
dotnet build -c Release | Out-Host

# --- PACK CORE: FastFsm.Net ---
Write-Host "`n[Pack FastFsm.Net => $CoreVersion]" -ForegroundColor Yellow
dotnet pack $CoreProj -c Release -p:PackageVersion=$CoreVersion -o $NugetDir | Out-Host

$CoreNupkg = Get-ChildItem "$NugetDir/FastFsm.Net.$CoreVersion.nupkg" -ErrorAction SilentlyContinue
if(-not $CoreNupkg){ throw "FastFsm.Net nupkg not found in $NugetDir" }

# --- INSPECT CORE NUSPEC ---
Write-Host "`n[Inspect FastFsm.Net .nuspec]" -ForegroundColor Yellow
$Temp = Join-Path $env:TEMP ("ffsm_diag_" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $Temp | Out-Null
Expand-Archive -Path $CoreNupkg.FullName -DestinationPath $Temp -Force

$Nuspec = Get-ChildItem "$Temp\*.nuspec" | Select-Object -First 1
if(-not $Nuspec){ throw "No .nuspec inside $($CoreNupkg.Name)" }

Write-Host "NUSPEC PATH: $($Nuspec.FullName)" -ForegroundColor DarkCyan
# pokaż dependencies, żeby złapać ewentualne 'Abstractions'
(Get-Content $Nuspec.FullName) |
  Select-String -Pattern '<dependencies>', '</dependencies>', 'dependency', 'Abstractions' -Context 0,2 |
  ForEach-Object { $_.Line } |
  Out-Host

# jasny komunikat, czy Abstractions jeszcze figuruje:
$hasAbstractions = Select-String -Path $Nuspec.FullName -Pattern 'id="Abstractions"' -SimpleMatch -Quiet
if($hasAbstractions){
  Write-Host ">>> FOUND dependency on 'Abstractions' in FastFsm.Net.nuspec (this is the root cause)" -ForegroundColor Red
} else {
  Write-Host ">>> OK: No 'Abstractions' dependency in FastFsm.Net.nuspec" -ForegroundColor Green
}

# --- PACK LOGGING: FastFsm.Net.Logging ---
Write-Host "`n[Pack FastFsm.Net.Logging => $LogVersion]" -ForegroundColor Yellow
dotnet pack $LogProj -c Release -p:PackageVersion=$LogVersion -o $NugetDir | Out-Host

$LogNupkg = Get-ChildItem "$NugetDir/FastFsm.Net.Logging.$LogVersion.nupkg" -ErrorAction SilentlyContinue
if($LogNupkg){
  Write-Host "Packed: $($LogNupkg.FullName)" -ForegroundColor Green
} else {
  Write-Host "Logging pack failed (check errors above)." -ForegroundColor Red
}

# --- DIAGNOSTYKA: csproj/props/nuget folder ---
Write-Host "`n[Diagnostics: csproj & props snippets]" -ForegroundColor Yellow

Write-Host "`n--- FastFsm.csproj: ProjectReference lines ---" -ForegroundColor DarkYellow
(Get-Content $CoreProj) | Select-String -Pattern '<ProjectReference', 'BuildOutputInPackage', 'TargetsForTfmSpecificBuildOutput' | ForEach-Object { $_.Line } | Out-Host

Write-Host "`n--- FastFsm.Logging.csproj: PackageReference & Content lines ---" -ForegroundColor DarkYellow
(Get-Content $LogProj) | Select-String -Pattern '<PackageReference', '<Content', 'ExtensionRunner.cs', 'PackageOutputPath' | ForEach-Object { $_.Line } | Out-Host

if(Test-Path $Props){
  Write-Host "`n--- FastFsm.Net.Logging.props ---" -ForegroundColor DarkYellow
  Get-Content $Props | Out-Host
} else {
  Write-Host "`n(Missing props file: $Props)" -ForegroundColor Red
}

Write-Host "`n[Check ExtensionRunner path]" -ForegroundColor Yellow
$runner1 = Join-Path $RepoRoot 'FastFsm.Logging\..\FastFsm\Runtime\Extensions\ExtensionRunner.cs'
$runner2 = Join-Path $RepoRoot 'FastFsm.Logging\..\StateMachine\Runtime\Extensions\ExtensionRunner.cs'
"{0} : {1}" -f $runner1, (Test-Path $runner1) | Out-Host
"{0} : {1}" -f $runner2, (Test-Path $runner2) | Out-Host

Write-Host "`n[nuget folder]" -ForegroundColor Yellow
Get-ChildItem $NugetDir -File | Sort-Object Name | Format-Table Name,Length,LastWriteTime -AutoSize

Write-Host "`n=== DONE ===" -ForegroundColor Cyan
