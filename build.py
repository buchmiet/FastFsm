#!/usr/bin/env python3
import argparse, re, subprocess, sys
from pathlib import Path
import xml.etree.ElementTree as ET

SEMVER_RE = re.compile(r'^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([0-9A-Za-z\-\.]+))?$')

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

def run(cmd, cwd=ROOT, fatal=True):
    # Wypisz komendę, przepuść pełny stdout/stderr polecenia (w tym .NET warningi/błędy),
    # ale bez Pythonowych tracebacków. Przy błędzie zakończ elegancko.
    print(">>", " ".join(cmd))
    proc = subprocess.run(cmd, cwd=cwd)
    if fatal and proc.returncode != 0:
        print(f"ERROR: command failed (exit {proc.returncode}): {' '.join(cmd)}")
        sys.exit(proc.returncode)
    return proc.returncode

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

    ver = pg.find("Version")
    if ver is None:
        ver = ET.SubElement(pg, "Version")
    ver.text = new_version

    pkgver = pg.find("PackageVersion")
    if pkgver is None:
        pkgver = ET.SubElement(pg, "PackageVersion")
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

def get_current_branch() -> str:
    try:
        out = subprocess.check_output(
            ["git", "rev-parse", "--abbrev-ref", "HEAD"],
            cwd=ROOT, text=True
        ).strip()
        return out
    except subprocess.CalledProcessError:
        return "unknown"

def sanitize_branch_for_prerelease(name: str) -> str:
    name = name.replace('/', '-').replace('_', '-').replace(' ', '-')
    name = re.sub(r'[^0-9A-Za-z\-.]', '-', name)
    name = re.sub(r'-{2,}', '-', name)
    name = name.strip('-.')
    return name or "branch"

def parse_semver(version: str):
    m = SEMVER_RE.match(version)
    if not m: 
        return None
    major, minor, patch, rev, pre = m.groups()
    return (int(major), int(minor), int(patch), int(rev) if rev else 0, pre or "")

def compare_versions(a: str, b: str) -> int:
    pa = parse_semver(a); pb = parse_semver(b)
    if pa is None and pb is None: return 0
    if pa is None: return -1
    if pb is None: return 1
    na = pa[:4]; nb = pb[:4]
    return (na > nb) - (na < nb)

def iter_local_nupkgs_for_id(package_id: str):
    if not NUGET_DIR.exists():
        return
    prefix = f"{package_id}."
    for p in NUGET_DIR.glob("*.nupkg"):
        name = p.name
        if not name.startswith(prefix):
            continue
        ver = name[len(prefix):]
        if ver.lower().endswith(".nupkg"):
            ver = ver[:-6]
        yield (ver, p)

def find_highest_version_for_branch(package_ids, branch_label: str) -> str:
    best = None
    for pkg_id in package_ids:
        for ver, _ in iter_local_nupkgs_for_id(pkg_id):
            parsed = parse_semver(ver)
            if not parsed:
                continue
            *nums, pre = parsed
            if pre == branch_label:
                if best is None or compare_versions(ver, best) > 0:
                    best = ver
    return best

def next_branch_version_from(seed: str) -> str:
    parsed = parse_semver(seed)
    if not parsed:
        return f"0.0.0.1-{seed}"
    major, minor, patch, rev, pre = parsed
    rev = (rev or 0) + 1
    return f"{major}.{minor}.{patch}.{rev}-{pre}"

def git_commit_and_tag(new_version: str, do_commit: bool, do_tag: bool):
    if not do_commit and not do_tag:
        return
    if do_commit:
        run(["git", "add", "-A"])
        run(["git", "commit", "-m", f"chore(release): v{new_version}"])
    if do_tag:
        # jeśli tag już istnieje – nie przerywaj release'u
        try:
            existing = subprocess.check_output(
                ["git", "tag", "-l", f"v{new_version}"], cwd=ROOT, text=True
            ).strip()
        except subprocess.CalledProcessError:
            existing = ""
        if existing:
            print(f"Tag v{new_version} already exists — skipping tag creation.")
        else:
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
    failed = []
    for csproj in find_csprojs():
        if is_test_project(csproj):
            rc = run(["dotnet", "test", str(csproj), "-c", configuration], fatal=False)
            if rc != 0:
                failed.append(csproj)
    if failed:
        print("\nTest failures:")
        for p in failed:
            print(" -", p.relative_to(ROOT))
        # zakończ bez tracebacka, ale z kodem błędu
        sys.exit(1)

def main():
    ap = argparse.ArgumentParser(description="Release builder for FastFsm (+DI + Logging) with wildcard test updates.")
    g = ap.add_mutually_exclusive_group()
    g.add_argument("--version", help="Ustaw wersję, np. 0.8.0.18")
    g.add_argument("--bump", choices=["patch","minor","major"], help="Podbij wersję od bieżącej (domyślnie patch).")
    ap.add_argument("--configuration", default="Release")
    ap.add_argument("--no-commit", action="store_true")
    ap.add_argument("--no-tag", action="store_true")
    ap.add_argument("--no-tests", action="store_true", help="Nie uruchamiaj dotnet test (po restore).")
    ap.add_argument("--no-branch-suffix", action="store_true",
                    help="Nie dołączaj nazwy gałęzi jako prerelease; klasyczne bump/--version.")
    ap.add_argument("--branch", help="Wymuś nazwę gałęzi (domyślnie pobierana z git).")
    args = ap.parse_args()

    # sanity
    for key, (_, proj) in PACKAGE_IDS.items():
        if not proj.exists():
            print(f"Brak projektu: {proj}", file=sys.stderr)
            sys.exit(1)

    current = parse_version_from_stamp(FASTFSM_PROJ)

    branch_name = args.branch.strip() if getattr(args, "branch", None) else get_current_branch()
    branch_label = sanitize_branch_for_prerelease(branch_name)
    use_branch_suffix = not args.no_branch_suffix

    if not use_branch_suffix:
        new_version = args.version.strip() if args.version else bump(current, args.bump or "patch")
    else:
        if args.version:
            base = args.version.strip()
            bparsed = parse_semver(base)
            if not bparsed:
                print(f"ERROR: Niepoprawna wersja: {base}", file=sys.stderr)
                sys.exit(2)
            major, minor, patch, rev, _ = bparsed
            rev = rev or 1
            new_version = f"{major}.{minor}.{patch}.{rev}-{branch_label}"
        else:
            ids = [PACKAGE_IDS["core"][0], PACKAGE_IDS["log"][0], PACKAGE_IDS["di"][0]]
            highest = find_highest_version_for_branch(ids, branch_label)
            if highest:
                new_version = next_branch_version_from(highest)
            else:
                new_version = f"0.0.0.1-{branch_label}"

    print(f"Gałąź: {branch_name}  → suffix: {branch_label}")
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
