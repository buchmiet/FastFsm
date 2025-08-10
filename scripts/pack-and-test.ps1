param(
  [string]$VersionPrefix = "0.7.0"
)

$ErrorActionPreference = "Stop"
$root = (Get-Location).Path
$nupkgs = Join-Path $root "nupkgs"
$scripts = Join-Path $root "scripts"

# 1) Clean nupkgs
if (Test-Path $nupkgs) { Remove-Item -Recurse -Force -Path $nupkgs }
New-Item -ItemType Directory -Path $nupkgs | Out-Null

# 2) Build & Pack StateMachine jako *-dev.*
$stamp = (Get-Date).ToString("yyyyMMddHHmmss")
$packageVersion = "$VersionPrefix-dev.$stamp"

dotnet clean "$root/StateMachine/StateMachine.csproj" -c Release
dotnet pack "$root/StateMachine/StateMachine.csproj" -c Release `
  -p:PackageVersion=$packageVersion `
  -p:ContinuousIntegrationBuild=true `
  -o $nupkgs

# 3) Wyczyść cache, przywróć i testuj
dotnet nuget locals http-cache --clear

# (opcjonalnie) pokaz co jest w nupkgs
Write-Host "Built packages:" -ForegroundColor Cyan
Get-ChildItem $nupkgs | Select-Object Name,Length | Format-Table | Out-String | Write-Host

# 4) Restore & Test – najpierw tests sync, potem async
$testProjects = @(
  "$root/StateMachine.Tests/StateMachine.Tests.csproj",
  "$root/StateMachine.Async.Tests/StateMachine.Async.Tests.csproj"
)

foreach ($proj in $testProjects) {
  Write-Host "`n=== RESTORE: $proj ===" -ForegroundColor Yellow
  dotnet restore $proj

  Write-Host "`n=== TEST: $proj ===" -ForegroundColor Yellow
  dotnet test $proj -c Release --no-build --logger "trx;LogFileName=$(Split-Path -Leaf $proj).trx"
}

Write-Host "`nAll done. Version used: $packageVersion" -ForegroundColor Green