# Pack FastFsm 0.9 packages and compile clean consumer consoles against ./nuget.
# Work directory is under TEMP so repo Directory.Build.props does not apply.
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$version = "0.9.0"
$feed = Join-Path $repo "nuget"

Write-Host "Packing product projects -> $feed"
foreach ($proj in @(
    "FastFsm\FastFsm.csproj",
    "FastFsm.Logging\FastFsm.Logging.csproj",
    "FastFsm.DependencyInjection\FastFsm.DependencyInjection.csproj")) {
    dotnet pack (Join-Path $repo $proj) -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed: $proj" }
}

foreach ($name in @(
    "FastFsm.Net.$version.nupkg",
    "FastFsm.Net.Logging.$version.nupkg",
    "FastFsm.Net.DependencyInjection.$version.nupkg")) {
    $pkg = Join-Path $feed $name
    if (-not (Test-Path $pkg)) { throw "Missing package: $pkg" }
    Write-Host "OK $name"
}

$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE ".nuget\packages" }
foreach ($id in @("fastfsm.net", "fastfsm.net.logging", "fastfsm.net.dependencyinjection")) {
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
            [string]$Program
        )
        $dir = Join-Path $work $Name
        New-Item -ItemType Directory -Path $dir | Out-Null
        Push-Location $dir
        try {
            dotnet new console -n $Name -f net10.0 --force
            if ($LASTEXITCODE -ne 0) { throw "dotnet new $Name failed" }
            Set-Location (Join-Path $dir $Name)
            Copy-Item (Join-Path $work "nuget.config") .
            foreach ($p in $Packages) {
                $pkgVersion = if ($p.StartsWith("Microsoft.")) { "10.0.11" } else { $version }
                dotnet add package $p --version $pkgVersion
                if ($LASTEXITCODE -ne 0) { throw "dotnet add package $p failed" }
            }
            Set-Content -Path "Machine.cs" -Value $Program -Encoding utf8
            Set-Content -Path "Program.cs" -Value "return App.Run();" -Encoding utf8
            dotnet build -c Release
            if ($LASTEXITCODE -ne 0) { throw "$Name build failed" }
            $out = & dotnet run -c Release --no-build | Out-String
            if ($LASTEXITCODE -ne 0) { throw "$Name run failed" }
            if ($out -notmatch [regex]::Escape("$Name-ok")) { throw "$Name unexpected output: $out" }
            Write-Host "SMOKE PASS $Name"
        }
        finally {
            Pop-Location
        }
    }

    Invoke-Smoke "core" @("FastFsm.Net") @'
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

    Invoke-Smoke "logging" @("FastFsm.Net", "FastFsm.Net.Logging", "Microsoft.Extensions.Logging.Abstractions") @'
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

    Invoke-Smoke "di" @(
        "FastFsm.Net",
        "FastFsm.Net.Logging",
        "FastFsm.Net.DependencyInjection",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Logging.Abstractions") @'
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

    Write-Host "All consumer smokes passed."
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
