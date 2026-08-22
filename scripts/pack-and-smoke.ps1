# Pack FastFsm 0.9 packages and compile clean consumer consoles against ./nuget.
# Work directory is under TEMP so repo Directory.Build.props does not apply.
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $repo "Directory.Build.props"
$versionMatch = Select-String -Path $propsPath -Pattern '<FastFsmPackageVersion>([^<]+)</FastFsmPackageVersion>'
if (-not $versionMatch) { throw "FastFsmPackageVersion not found in Directory.Build.props" }
$version = $versionMatch.Matches[0].Groups[1].Value
$feed = Join-Path $repo "nuget"

Write-Host "Packing product projects -> $feed"
foreach ($proj in @(
    "src/Fsm/Fsm.Core/Fsm.Core.csproj",
    "src/Fsm/Fsm.Logging/Fsm.Logging.csproj",
    "src/Fsm/Fsm.DependencyInjection/Fsm.DependencyInjection.csproj",
    "src/Fsm/Fsm.Observability/Fsm.Observability.csproj")) {
    dotnet build (Join-Path $repo $proj) -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed: $proj" }
}

Write-Host "Packing legacy metapackages (FastFsm.Net* -> FastFsm.Sharp*) -> $feed"
$nugetConfig = Join-Path $repo "nuget.config"
foreach ($proj in @(
    "src/LegacyPackages/FastFsm.Net/FastFsm.Net.csproj",
    "src/LegacyPackages/FastFsm.Net.Logging/FastFsm.Net.Logging.csproj",
    "src/LegacyPackages/FastFsm.Net.DependencyInjection/FastFsm.Net.DependencyInjection.csproj")) {
    dotnet pack (Join-Path $repo $proj) -c Release --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed: $proj" }
}

foreach ($name in @(
    "FastFsm.Sharp.$version.nupkg",
    "FastFsm.Sharp.Logging.$version.nupkg",
    "FastFsm.Sharp.DependencyInjection.$version.nupkg",
    "FastFsm.Sharp.Observability.$version.nupkg",
    "FastFsm.Net.$version.nupkg",
    "FastFsm.Net.Logging.$version.nupkg",
    "FastFsm.Net.DependencyInjection.$version.nupkg")) {
    $pkg = Join-Path $feed $name
    if (-not (Test-Path $pkg)) { throw "Missing package: $pkg" }
    Write-Host "OK $name"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-NuspecXml {
    param([string]$Nupkg)
    $z = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $Nupkg))
    try {
        $e = $z.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
        if (-not $e) { throw "No nuspec in $Nupkg" }
        $sr = New-Object IO.StreamReader($e.Open())
        try { return [xml]$sr.ReadToEnd() } finally { $sr.Dispose() }
    }
    finally { $z.Dispose() }
}

function Get-NuspecDependencyIds {
    param([xml]$Nuspec)
    $ns = New-Object Xml.XmlNamespaceManager($Nuspec.NameTable)
    $ns.AddNamespace("n", $Nuspec.DocumentElement.NamespaceURI)
    @($Nuspec.SelectNodes("//n:dependency", $ns) | ForEach-Object { $_.GetAttribute("id") })
}

function Assert-NupkgDependsOn {
    param([string]$Nupkg, [string[]]$Ids)
    $xml = Get-NuspecXml $Nupkg
    $have = Get-NuspecDependencyIds $xml
    foreach ($id in $Ids) {
        if ($have -notcontains $id) {
            throw "$Nupkg missing NuGet dependency '$id'. Have: $($have -join ', ')"
        }
    }
    if ($have -contains "Abstractions") {
        throw "$Nupkg must not depend on unpublished Abstractions"
    }
    Write-Host "OK deps $($Ids -join ', ') in $(Split-Path $Nupkg -Leaf)"
}

Assert-NupkgDependsOn (Join-Path $feed "FastFsm.Sharp.Logging.$version.nupkg") @(
    "FastFsm.Sharp", "Microsoft.Extensions.Logging.Abstractions")
Assert-NupkgDependsOn (Join-Path $feed "FastFsm.Sharp.DependencyInjection.$version.nupkg") @(
    "FastFsm.Sharp", "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.Logging.Abstractions")
Assert-NupkgDependsOn (Join-Path $feed "FastFsm.Sharp.Observability.$version.nupkg") @(
    "FastFsm.Sharp", "Microsoft.Extensions.Logging.Abstractions", "Microsoft.Extensions.DependencyInjection.Abstractions")

$coreNupkg = Join-Path $feed "FastFsm.Sharp.$version.nupkg"
$dllTmp = Join-Path ([IO.Path]::GetTempPath()) ("FastFsm-asmcheck-" + [guid]::NewGuid().ToString("n") + ".dll")
$z = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $coreNupkg))
try {
    $e = $z.Entries | Where-Object { $_.FullName -replace '\\','/' -eq 'lib/net10.0/FastFsm.dll' } | Select-Object -First 1
    if (-not $e) { throw "FastFsm.dll missing from $coreNupkg" }
    $fs = [IO.File]::Create($dllTmp)
    try { $e.Open().CopyTo($fs) } finally { $fs.Dispose() }
}
finally { $z.Dispose() }
$asmVersion = [Reflection.AssemblyName]::GetAssemblyName($dllTmp).Version
Remove-Item $dllTmp -Force
$expectedAsm = [Version]"$version.0"
if ($asmVersion -ne $expectedAsm) {
    throw "FastFsm.dll AssemblyVersion is $asmVersion, expected $expectedAsm"
}
Write-Host "OK FastFsm.dll AssemblyVersion $asmVersion"


$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE ".nuget\packages" }
foreach ($id in @("fastfsm.sharp", "fastfsm.sharp.logging", "fastfsm.sharp.dependencyinjection")) {
    $cached = Join-Path $nugetRoot $id
    if (Test-Path $cached) { Remove-Item -Recurse -Force $cached }
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("fastfsm-smoke-" + [guid]::NewGuid().ToString("n"))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="fastfsm-local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -Path (Join-Path $work "nuget.config") -Encoding UTF8

    function Invoke-Smoke {
        param(
            [string]$Name,
            [string[]]$Packages,
            [string]$Program,
            [string]$TargetFramework = 'net10.0'
        )
        $dir = Join-Path $work $Name
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Push-Location $dir
        try {
            if ($TargetFramework -eq 'net10.0') {
                dotnet new console -n $Name -f net10.0 --force
                if ($LASTEXITCODE -ne 0) { throw "dotnet new $Name failed" }
                Set-Location (Join-Path $dir $Name)
            } else {
                New-Item -ItemType Directory -Path $Name -Force | Out-Null
                Set-Location (Join-Path $dir $Name)
                @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$TargetFramework</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
"@ | Set-Content -Path "$Name.csproj" -Encoding utf8
            }
            Copy-Item (Join-Path $work "nuget.config") .
            foreach ($p in $Packages) {
                dotnet add package $p --version $version
                if ($LASTEXITCODE -ne 0) { throw "dotnet add package $p failed" }
            }
            Set-Content -Path "Machine.cs" -Value $Program -Encoding utf8
            Set-Content -Path "Program.cs" -Value "return App.Run();" -Encoding utf8
            dotnet build -c Release
            if ($LASTEXITCODE -ne 0) { throw "$Name build failed" }
            $out = & dotnet run -c Release --no-build | Out-String
            if ($LASTEXITCODE -ne 0) { throw "$Name run failed" }
            if ($out -notmatch [regex]::Escape("$Name-ok")) { throw "$Name unexpected output: $out" }
            Write-Host "SMOKE PASS $Name ($TargetFramework)"
        }
        finally {
            Pop-Location
        }
    }

    $coreProg = @'
using Abstractions.Attributes;

public enum S { Off, On }
public enum T { Toggle }

[StateMachine(typeof(S), typeof(T))]
public partial class Light
{
    [Transition(S.Off, T.Toggle, S.On)]
    [Transition(S.On, T.Toggle, S.Off)]
    private void Configure() { }
}

static class App
{
    public static int Run()
    {
        var m = new Light(S.Off);
        m.Start();
        if (!m.TryFire(T.Toggle) || m.CurrentState != S.On)
            throw new System.Exception("core transition failed");
        System.Console.WriteLine("core-ok");
        return 0;
    }
}
'@

    $loggingProg = @'
using Abstractions.Attributes;
using Microsoft.Extensions.Logging.Abstractions;

public enum S { Off, On }
public enum T { Toggle }

[StateMachine(typeof(S), typeof(T))]
public partial class Light
{
    [Transition(S.Off, T.Toggle, S.On)]
    [Transition(S.On, T.Toggle, S.Off)]
    private void Configure() { }
}

static class App
{
    public static int Run()
    {
        var m = new Light(S.Off, NullLogger<Light>.Instance);
        m.Start();
        if (!m.TryFire(T.Toggle) || m.CurrentState != S.On)
            throw new System.Exception("logging transition failed");
        System.Console.WriteLine("logging-ok");
        return 0;
    }
}
'@

    $diProg = @'
using Abstractions.Attributes;
using FastFsm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

public enum S { Off, On }
public enum T { Toggle }

[StateMachine(typeof(S), typeof(T))]
public partial class Light
{
    [Transition(S.Off, T.Toggle, S.On)]
    [Transition(S.On, T.Toggle, S.Off)]
    private void Configure() { }
}

static class App
{
    public static int Run()
    {
        var services = new ServiceCollection();
        services.AddStateMachine<ILight, Light, S, T>();
        using var sp = services.BuildServiceProvider();
        var m = sp.GetRequiredService<ILight>();
        m.Start();
        if (!m.TryFire(T.Toggle) || m.CurrentState != S.On)
            throw new System.Exception("di transition failed");
        System.Console.WriteLine("di-ok");
        return 0;
    }
}
'@

    Invoke-Smoke "core" @("FastFsm.Sharp") $coreProg
    Invoke-Smoke "logging" @("FastFsm.Sharp.Logging") $loggingProg
    Invoke-Smoke "legacy-core" @("FastFsm.Net") ($coreProg.Replace("core-ok", "legacy-core-ok"))
    Invoke-Smoke "di" @("FastFsm.Sharp.DependencyInjection") $diProg

    Invoke-Smoke "core-win" @("FastFsm.Sharp") ($coreProg.Replace("core-ok", "core-win-ok")) -TargetFramework 'net10.0-windows'
    Invoke-Smoke "logging-win" @("FastFsm.Sharp.Logging") ($loggingProg.Replace("logging-ok", "logging-win-ok")) -TargetFramework 'net10.0-windows'
    Invoke-Smoke "di-win" @("FastFsm.Sharp.DependencyInjection") ($diProg.Replace("di-ok", "di-win-ok")) -TargetFramework 'net10.0-windows'

    Write-Host "All consumer smokes passed."
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
