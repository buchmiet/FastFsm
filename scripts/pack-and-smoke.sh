#!/usr/bin/env bash
# Pack FastFsm 0.9 packages and compile clean consumer consoles against ./nuget.
# Work directory is under TMPDIR so repo Directory.Build.props does not apply.
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="$(sed -n 's:.*<FastFsmPackageVersion>\([^<]*\)</FastFsmPackageVersion>.*:\1:p' "$REPO/Directory.Build.props" | head -n 1)"
if [[ -z "$VERSION" ]]; then
  echo "FastFsmPackageVersion not found in Directory.Build.props" >&2
  exit 1
fi
FEED="$REPO/nuget"

echo "Packing product projects -> $FEED"
# GeneratePackageOnBuild on product projects; dotnet pack alone can ship stale assemblies.
dotnet build "$REPO/src/Fsm/Fsm.Core/Fsm.Core.csproj" -c Release
dotnet build "$REPO/src/Fsm/Fsm.Logging/Fsm.Logging.csproj" -c Release
dotnet build "$REPO/src/Fsm/Fsm.DependencyInjection/Fsm.DependencyInjection.csproj" -c Release

echo "Packing legacy metapackages (FastFsm.Net* -> FastFsm.Sharp*) -> $FEED"
dotnet pack "$REPO/src/LegacyPackages/FastFsm.Net/FastFsm.Net.csproj" -c Release --configfile "$REPO/nuget.config"
dotnet pack "$REPO/src/LegacyPackages/FastFsm.Net.Logging/FastFsm.Net.Logging.csproj" -c Release --configfile "$REPO/nuget.config"
dotnet pack "$REPO/src/LegacyPackages/FastFsm.Net.DependencyInjection/FastFsm.Net.DependencyInjection.csproj" -c Release --configfile "$REPO/nuget.config"

for pkg in \
  "FastFsm.Sharp.$VERSION.nupkg" \
  "FastFsm.Sharp.Logging.$VERSION.nupkg" \
  "FastFsm.Sharp.DependencyInjection.$VERSION.nupkg" \
  "FastFsm.Net.$VERSION.nupkg" \
  "FastFsm.Net.Logging.$VERSION.nupkg" \
  "FastFsm.Net.DependencyInjection.$VERSION.nupkg"
do
  if [[ ! -f "$FEED/$pkg" ]]; then
    echo "Missing package: $FEED/$pkg" >&2
    exit 1
  fi
  echo "OK $pkg"
done

python3 - "$FEED" "$VERSION" <<'PY'
import sys, zipfile, xml.etree.ElementTree as ET
from pathlib import Path

feed, version = Path(sys.argv[1]), sys.argv[2]

def nuspec_ids(nupkg):
    with zipfile.ZipFile(nupkg) as z:
        name = next(n for n in z.namelist() if n.endswith(".nuspec"))
        root = ET.fromstring(z.read(name))
    ns = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}
    path = ".//n:dependency" if ns else ".//dependency"
    return [el.get("id") for el in root.findall(path, ns)]

def require(nupkg, ids):
    have = nuspec_ids(nupkg)
    missing = [i for i in ids if i not in have]
    if missing:
        raise SystemExit(f"{nupkg.name} missing {missing}; have {have}")
    if "Abstractions" in have:
        raise SystemExit(f"{nupkg.name} must not depend on Abstractions")
    print(f"OK deps {', '.join(ids)} in {nupkg.name}")

require(feed / f"FastFsm.Sharp.Logging.{version}.nupkg",
        ["FastFsm.Sharp", "Microsoft.Extensions.Logging.Abstractions"])
require(feed / f"FastFsm.Sharp.DependencyInjection.{version}.nupkg",
        ["FastFsm.Sharp", "Microsoft.Extensions.DependencyInjection",
         "Microsoft.Extensions.Logging.Abstractions"])

core = feed / f"FastFsm.Sharp.{version}.nupkg"
with zipfile.ZipFile(core) as z:
    data = z.read("lib/net10.0/FastFsm.dll")
version_ascii = version.encode("ascii")
version_utf16 = version.encode("utf-16le")
if b"1.0.0.0" in data and version_ascii not in data and version_utf16 not in data:
    raise SystemExit("FastFsm.dll still looks like 1.0.0.0")
if version_ascii not in data and version_utf16 not in data:
    raise SystemExit(f"FastFsm.dll does not contain {version}")
print(f"OK FastFsm.dll embeds {version}")
PY

NUGET_ROOT="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
rm -rf "$NUGET_ROOT/fastfsm.sharp" "$NUGET_ROOT/fastfsm.sharp.logging" "$NUGET_ROOT/fastfsm.sharp.dependencyinjection"

WORK="$(mktemp -d "${TMPDIR:-/tmp}/fastfsm-smoke.XXXXXX")"
cleanup() { rm -rf "$WORK"; }
trap cleanup EXIT

cat > "$WORK/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="fastfsm-local" value="$FEED" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

write_and_run() {
  local name="$1"
  local program="$2"
  local tfm="${3:-net10.0}"
  shift 3
  local dir="$WORK/$name"
  mkdir -p "$dir"
  pushd "$dir" >/dev/null
  if [[ "$tfm" == "net10.0" ]]; then
    dotnet new console -n "$name" -f net10.0 --force
    cd "$name"
  else
    mkdir -p "$name"
    cd "$name"
    cat > "$name.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$tfm</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
EOF
  fi
  cp "$WORK/nuget.config" .
  for p in "$@"; do
    dotnet add package "$p" --version "$VERSION"
  done
  printf '%s\n' "$program" > Machine.cs
  printf '%s\n' 'return App.Run();' > Program.cs
  dotnet build -c Release
  local out
  out="$(dotnet run -c Release --no-build)"
  echo "$out"
  echo "$out" | grep -q "${name}-ok"
  echo "SMOKE PASS $name ($tfm)"
  popd >/dev/null
}

CORE_PROG='using Abstractions.Attributes;

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
}'

LOG_PROG='using Abstractions.Attributes;
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
}'

DI_PROG='using Abstractions.Attributes;
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
}'

LEGACY_CORE_PROG="${CORE_PROG/core-ok/legacy-core-ok}"
write_and_run core "$CORE_PROG" net10.0 FastFsm.Sharp
write_and_run logging "$LOG_PROG" net10.0 FastFsm.Sharp.Logging
write_and_run legacy-core "$LEGACY_CORE_PROG" net10.0 FastFsm.Net
write_and_run di "$DI_PROG" net10.0 FastFsm.Sharp.DependencyInjection

CORE_WIN_PROG="${CORE_PROG/core-ok/core-win-ok}"
LOG_WIN_PROG="${LOG_PROG/logging-ok/logging-win-ok}"
DI_WIN_PROG="${DI_PROG/di-ok/di-win-ok}"
write_and_run core-win "$CORE_WIN_PROG" net10.0-windows FastFsm.Sharp
write_and_run logging-win "$LOG_WIN_PROG" net10.0-windows FastFsm.Sharp.Logging
write_and_run di-win "$DI_WIN_PROG" net10.0-windows FastFsm.Sharp.DependencyInjection

echo "All consumer smokes passed."
