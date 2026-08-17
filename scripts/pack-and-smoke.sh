#!/usr/bin/env bash
# Pack FastFsm 0.9 packages and compile clean consumer consoles against ./nuget.
# Work directory is under TMPDIR so repo Directory.Build.props does not apply.
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="0.9.0"
FEED="$REPO/nuget"

echo "Packing product projects -> $FEED"
# Pack only the three nupkgs. Do not pass GeneratePackageOnBuild=true on the
# solution — that overrides Generator.csproj and trips a Pack cycle on SDK 10.
dotnet pack "$REPO/FastFsm/FastFsm.csproj" -c Release
dotnet pack "$REPO/FastFsm.Logging/FastFsm.Logging.csproj" -c Release
dotnet pack "$REPO/FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj" -c Release

for pkg in \
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

require(feed / f"FastFsm.Net.Logging.{version}.nupkg",
        ["FastFsm.Net", "Microsoft.Extensions.Logging.Abstractions"])
require(feed / f"FastFsm.Net.DependencyInjection.{version}.nupkg",
        ["FastFsm.Net", "Microsoft.Extensions.DependencyInjection",
         "Microsoft.Extensions.Logging.Abstractions"])

core = feed / f"FastFsm.Net.{version}.nupkg"
with zipfile.ZipFile(core) as z:
    data = z.read("lib/net10.0/FastFsm.dll")
if b"1.0.0.0" in data and b"0.9.0" not in data and "0.9.0".encode("utf-16le") not in data:
    raise SystemExit("FastFsm.dll still looks like 1.0.0.0")
if b"0.9.0" not in data and "0.9.0".encode("utf-16le") not in data:
    raise SystemExit("FastFsm.dll does not contain 0.9.0")
print("OK FastFsm.dll embeds 0.9.0")
PY

NUGET_ROOT="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
rm -rf "$NUGET_ROOT/fastfsm.net" "$NUGET_ROOT/fastfsm.net.logging" "$NUGET_ROOT/fastfsm.net.dependencyinjection"

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
  shift 2
  local dir="$WORK/$name/$name"
  mkdir -p "$WORK/$name"
  pushd "$WORK/$name" >/dev/null
  dotnet new console -n "$name" -f net10.0 --force
  cd "$name"
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
  echo "SMOKE PASS $name"
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

write_and_run core "$CORE_PROG" FastFsm.Net
write_and_run logging "$LOG_PROG" FastFsm.Net.Logging
write_and_run di "$DI_PROG" FastFsm.Net.DependencyInjection

echo "All consumer smokes passed."
