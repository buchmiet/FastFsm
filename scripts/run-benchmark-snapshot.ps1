#Requires -Version 7.0
<#
.SYNOPSIS
  Pack FastFsm.Sharp and run the full BenchmarkDotNet suite in packaged (consumer) mode.

.PARAMETER HostLabel
  Neutral hardware label for docs (e.g. win-x64-amd-9600x). Required when -CopyToDocs is set.

.PARAMETER Filter
  Optional BenchmarkDotNet filter passed to the benchmark exe.

.PARAMETER CopyToDocs
  When set, copies github markdown reports into docs/benchmarks/results/{HostLabel}-{date}.md
#>
param(
    [string]$Filter = "*",
    [string]$HostLabel = "",
    [switch]$CopyToDocs
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

$date = Get-Date -Format "yyyy-MM-dd"
$benchDir = Join-Path $repoRoot "src/Benchmark"
$artifactDir = Join-Path $benchDir "BenchmarkDotNet.Artifacts/results"

Write-Host "Packing FastFsm.Sharp..." -ForegroundColor Cyan
dotnet pack (Join-Path $repoRoot "src/Fsm/Fsm.Core/Fsm.Core.csproj") -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building generator and benchmark projects..." -ForegroundColor Cyan
dotnet build (Join-Path $repoRoot "src/Generator/Generator.Core/Generator.Core.csproj") -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build (Join-Path $repoRoot "src/Fsm/Fsm.Core/Fsm.Core.csproj") -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build (Join-Path $benchDir "Benchmark.csproj") -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running benchmarks (filter: $Filter)..." -ForegroundColor Cyan
$args = @("-c", "Release", "-p:UsePackages=true", "--project", (Join-Path $benchDir "Benchmark.csproj"), "--", "--filter", $Filter)
dotnet run @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $CopyToDocs) {
    Write-Host "Reports: $artifactDir"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($HostLabel)) {
    throw "HostLabel is required with -CopyToDocs"
}

$commit = (git rev-parse --short HEAD).Trim()
$dotnetInfo = (dotnet --info) -join "`n"
$outFile = Join-Path $repoRoot "docs/benchmarks/results/$HostLabel-$date.md"

$reports = Get-ChildItem $artifactDir -Filter "*-report-github.md" | Sort-Object Name
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("# Benchmark snapshot: ``$HostLabel``")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Field | Value |")
[void]$sb.AppendLine("|-------|-------|")
[void]$sb.AppendLine("| Date | $date |")
[void]$sb.AppendLine("| Commit | ``$commit`` |")
[void]$sb.AppendLine("| Package | FastFsm.Sharp $(Select-Xml -Path (Join-Path $repoRoot 'Directory.Build.props') -XPath '//FastFsmPackageVersion' | ForEach-Object { $_.Node.InnerText }) |")
[void]$sb.AppendLine("| Mode | ``UsePackages=true`` (packaged consumer) |")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Environment")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("``````")
[void]$sb.AppendLine($dotnetInfo.Trim())
[void]$sb.AppendLine("``````")
[void]$sb.AppendLine("")

foreach ($report in $reports) {
    [void]$sb.AppendLine("## $($report.BaseName -replace '-report-github$','')")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine((Get-Content $report.FullName -Raw))
    [void]$sb.AppendLine("")
}

Set-Content -Path $outFile -Value $sb.ToString() -Encoding utf8
Write-Host "Wrote $outFile" -ForegroundColor Green
