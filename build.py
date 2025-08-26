#!/usr/bin/env python3
import argparse, re, subprocess, sys
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent
NUGET_DIR = ROOT / "nuget"

FASTFSM_PROJ = ROOT / "FastFsm" / "FastFsm.csproj"
LOGGING_PROJ = ROOT / "FastFsm.Logging" / "FastFsm.Logging.csproj"
DI_PROJ      = ROOT / "FastFsm.DependencyInjection" / "FastFsm.DependencyInjection.csproj"

PACKAGE_IDS = {
    "core":  ("FastFsm.Net", FASTFSM_PROJ),
    "log":   ("FastFsm.Net.Logging", LOGGING_PROJ),
    "di":    ("FastFsm.Net.DependencyInjection", DI_PROJ),
}

def run(cmd, cwd=ROOT):
    print(">>", " ".join(cmd))
    subprocess.check_call(cmd, cwd=cwd)

def parse_version_from_stamp(csproj: Path) -> str:
    tree = ET.parse(csproj)
    root = tree.getroot()
    for t in root.findall("./Target"):
        if t.get("Name") == "StampVersionForNupkg":
            for pg in t.findall("./PropertyGroup"):
                ver = pg.find("Version")
                if ver is not None and ver.text:
                    return ver.text.strip()
    # fallback
    ver = root.find(".//Version")
    if ver is None or not (ver.text or "").strip():
        raise RuntimeError(f"Nie znaleziono <Version> w {csproj}")
    return ver.text.strip()

def set_version_in_stamp(csproj: Path, new_version: str):
    tree = ET.parse(csproj)
    root = tree.getroot()
    target = None
    for t in root.findall("./Target"):
        if t.get("Name") == "StampVersionForNupkg":
            target = t
            break
    if target is None:
        target = ET.SubElement(root, "Target", {"Name":"StampVersionForNupkg","BeforeTargets":"GenerateNuspec"})
        ET.SubElement(target, "PropertyGroup")
    pg = target.find("./PropertyGroup")
    if pg is None:
        pg = ET.SubElement(target, "PropertyGroup")

    ver = pg.find("Version") or ET.SubElement(pg, "Version")
    ver.text = new_version

    pkgver = pg.find("PackageVersion") or ET.SubElement(pg, "PackageVersion")
    # trzymajmy PackageVersion spięte z $(Version) – nupkg dostanie to samo
    pkgver.text = "$(Version)"

    tree.write(csproj, encoding="utf-8", xml_declaration=True)

def bump(ver: str, which: str) -> str:
    parts = [int(x) for x in ver.split(".")]
    while len(parts) < 3: parts.append(0)
    if which == "patch":
        parts[-1] += 1
    elif which == "minor":
        parts[-2] += 1; parts[-1] = 0
    elif which == "major":
        parts[0] += 1;  parts[1:] = [0]*(len(parts)-1)
    else:
        raise ValueError(which)
    return ".".join(map(str, parts))

def update_packageref(csproj: Path, include_id: str, new_version: str) -> bool:
    tree = ET.parse(csproj)
    root = tree.getroot()
    changed = False
    for pr in root.findall(".//PackageReference"):
        if pr.get("Include") == include_id:
            if pr.get("Version") != new_version:
                pr.set("Version", new_version)
                changed = True
    if changed:
        tree.write(csproj, encoding="utf-8", xml_declaration=True)
    return changed

def find_csprojs(pattern=None):
    return list(ROOT.glob("**/*.csproj"))

def is_test_project(csproj: Path) -> bool:
    # folder nazwy: ^Fast.*\.Tests$
    return re.match(r"^Fast.*\.Tests$", csproj.parent.name) is not None

def update_tests_versions(new_version: str):
    touched = []
    for csproj in find_csprojs():
        if is_test_project(csproj):
            c1 = update_packageref(csproj, "FastFsm.Net", new_version)
            c2 = update_packageref(csproj, "FastFsm.Net.Logging", new_version)
            c3 = update_packageref(csproj, "FastFsm.Net.DependencyInjection", new_version)
            if c1 or c2 or c3:
                touched.append(csproj)
    if touched:
        print("Zaktualizowano wersje w testach:")
        for p in touched:
            print("  -", p.relative_to(ROOT))

def git_commit_and_tag(new_version: str, do_commit: bool, do_tag: bool):
    if not do_commit and not do_tag:
        return
    if do_commit:
        run(["git", "add", "-A"])
        run(["git", "commit", "-m", f"chore(release): v{new_version}"])
    if do_tag:
        run(["git", "tag", "-a", f"v{new_version}", "-m", f"v{new_version}"])

def dotnet_pack(csproj: Path, configuration: str):
    run(["dotnet", "pack", str(csproj), "-c", configuration, "-o", str(NUGET_DIR)])

def restore_tests_with_local():
    for csproj in find_csprojs():
        if is_test_project(csproj):
            run(["dotnet", "restore", str(csproj),
                 "--source", str(NUGET_DIR),
                 "--source", "https://api.nuget.org/v3/index.json"])

def run_tests(configuration: str):
    for csproj in find_csprojs():
        if is_test_project(csproj):
            run(["dotnet", "test", str(csproj), "-c", configuration, "--no-build"])

def main():
    ap = argparse.ArgumentParser(description="Release builder for FastFsm (+DI + Logging) with wildcard test updates.")
    g = ap.add_mutually_exclusive_group()
    g.add_argument("--version", help="Ustaw wersję, np. 0.8.0.18")
    g.add_argument("--bump", choices=["patch","minor","major"], help="Podbij wersję od bieżącej (domyślnie patch).")
    ap.add_argument("--configuration", default="Release")
    ap.add_argument("--no-commit", action="store_true")
    ap.add_argument("--no-tag", action="store_true")
    ap.add_argument("--no-tests", action="store_true", help="Nie uruchamiaj dotnet test (po restore).")
    args = ap.parse_args()

    # sanity
    for key, (_, proj) in PACKAGE_IDS.items():
        if not proj.exists():
            print(f"Brak projektu: {proj}", file=sys.stderr)
            sys.exit(1)

    current = parse_version_from_stamp(FASTFSM_PROJ)
    new_version = args.version.strip() if args.version else bump(current, args.bump or "patch")
    print(f"Wersja: {current} -> {new_version}")

    # 1) ustaw wersję w 3 paczkach
    set_version_in_stamp(FASTFSM_PROJ, new_version)
    set_version_in_stamp(LOGGING_PROJ, new_version)
    set_version_in_stamp(DI_PROJ,      new_version)

    # 2) podmień referencje na FastFsm.Net w DI i Logging
    update_packageref(DI_PROJ,      "FastFsm.Net", new_version)
    update_packageref(LOGGING_PROJ, "FastFsm.Net", new_version)

    # 3) testy (wildcard Fast*.Tests)
    update_tests_versions(new_version)

    # 4) commit + tag
    git_commit_and_tag(new_version, do_commit=not args.no_commit, do_tag=not args.no_tag)

    # 5) pack: Core -> DI -> Logging (w tej kolejności)
    NUGET_DIR.mkdir(exist_ok=True)
    dotnet_pack(FASTFSM_PROJ, args.configuration)
    dotnet_pack(DI_PROJ,      args.configuration)
    dotnet_pack(LOGGING_PROJ, args.configuration)

    # 6) restore testów z lokalnego feeda
    restore_tests_with_local()

    # 7) opcjonalnie odpal testy
    if not args.no_tests:
        run_tests(args.configuration)

    print("\nGotowe. Paczki w:", NUGET_DIR)

if __name__ == "__main__":
    main()
