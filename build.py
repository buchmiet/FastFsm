#!/usr/bin/env python3
"""Build script with advanced TUI support"""
import argparse, re, subprocess, sys
from pathlib import Path
import xml.etree.ElementTree as ET
from typing import List, Optional

# Import TUI system
sys.path.insert(0, str(Path(__file__).parent))
from tui import create_tui, ITui

# Constants
SEMVER_RE = re.compile(r'^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([0-9A-Za-z\-\.]+))?$')
ROOT = Path(__file__).resolve().parent
NUGET_DIR = ROOT / "nuget"

FASTFSM_PROJ = ROOT / "FastFsm" / "FastFsm.csproj"
LOGGING_PROJ = ROOT / "FastFsm.Logging" / "FastFsm.Logging.csproj"
DI_PROJ = ROOT / "FastFsm.DependencyInjection" / "FastFsm.DependencyInjection.csproj"

PACKAGE_IDS = {
    "core": ("FastFsm.Net", FASTFSM_PROJ),
    "log": ("FastFsm.Net.Logging", LOGGING_PROJ),
    "di": ("FastFsm.Net.DependencyInjection", DI_PROJ),
}

# Warning/error detection
_MS_WARN_RE = re.compile(r'\bwarning\s+(CS|NU|NETSDK|MSB)\w*[: ]', re.IGNORECASE)
_MS_ERR_RE = re.compile(r'\berror\s+(CS|NU|NETSDK|MSB)\w*[: ]', re.IGNORECASE)

def _is_warning_line(s: str) -> bool:
    return bool(_MS_WARN_RE.search(s) or ': warning ' in s.lower())

def _is_error_line(s: str) -> bool:
    return bool(_MS_ERR_RE.search(s) or ': error ' in s.lower())

# Global UI instance
_ui: Optional[ITui] = None
_show_warnings = False

def run(cmd, cwd=ROOT, fatal=True, label=None):
    """Run command with TUI integration"""
    global _ui, _show_warnings
    
    if label and _ui:
        _ui.start(label)
    
    warn_count = 0
    err_count = 0
    
    proc = subprocess.Popen(
        cmd, cwd=cwd,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True, bufsize=1
    )
    
    assert proc.stdout is not None
    for raw in proc.stdout:
        line = raw.rstrip("\n")
        if _is_error_line(line):
            err_count += 1
            print(line)  # Always show errors
        elif _is_warning_line(line):
            warn_count += 1
            if _show_warnings:
                print(line)
        else:
            # Only print other output if not using TUI
            if not _ui or not label:
                print(line)
        
        # Update TUI
        if label and _ui:
            _ui.update(label, warn_count, err_count)
    
    proc.wait()
    rc = proc.returncode
    
    if label and _ui:
        _ui.finish(label, rc == 0, warn_count, err_count)
    
    if fatal and rc != 0:
        print(f"ERROR: command failed (exit {rc}): {' '.join(cmd)}")
        sys.exit(rc)
    
    return rc

# Version management functions
def parse_version_from_stamp(csproj: Path) -> str:
    tree = ET.parse(csproj)
    root = tree.getroot()
    for t in root.findall("./Target"):
        if t.get("Name") == "StampVersionForNupkg":
            for pg in t.findall("./PropertyGroup"):
                ver = pg.find("Version")
                if ver is not None and ver.text:
                    return ver.text.strip()
    ver = root.find(".//Version")
    if ver is None or not (ver.text or "").strip():
        raise RuntimeError(f"No <Version> found in {csproj}")
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
        parts[0] += 1; parts[1:] = [0]*(len(parts)-1)
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

# Branch versioning functions
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

# Build task functions
def find_csprojs(pattern=None):
    return list(ROOT.glob("**/*.csproj"))

def is_test_project(csproj: Path) -> bool:
    return re.match(r"^Fast.*\.Tests$", csproj.parent.name) is not None

def collect_tasks(args) -> List[str]:
    """Collect all build tasks that will be executed"""
    tasks = []
    
    if not args.no_commit and not args.no_tag:
        if not args.no_commit:
            tasks.append("git commit")
        if not args.no_tag:
            tasks.append("git tag")
    
    # Pack tasks
    tasks.append("pack FastFsm")
    tasks.append("pack FastFsm.DependencyInjection")
    tasks.append("pack FastFsm.Logging")
    
    # Restore tasks
    test_projects = [p for p in find_csprojs() if is_test_project(p)]
    for csproj in test_projects:
        name = csproj.parent.name
        tasks.append(f"restore {name}")
    
    # Test tasks
    if not args.no_tests:
        for csproj in test_projects:
            name = csproj.parent.name
            tasks.append(f"test {name}")
    
    return tasks

def update_tests_versions(new_version: str):
    """Update package references in test projects"""
    touched = []
    for csproj in find_csprojs():
        if is_test_project(csproj):
            c1 = update_packageref(csproj, "FastFsm.Net", new_version)
            c2 = update_packageref(csproj, "FastFsm.Net.Logging", new_version)
            c3 = update_packageref(csproj, "FastFsm.Net.DependencyInjection", new_version)
            if c1 or c2 or c3:
                touched.append(csproj)
    if touched:
        print("Updated versions in tests:")
        for p in touched:
            print("  -", p.relative_to(ROOT))

def git_commit_and_tag(new_version: str, do_commit: bool, do_tag: bool):
    """Git operations with TUI integration"""
    if not do_commit and not do_tag:
        return
    
    if do_commit:
        run(["git", "add", "-A"], label="git commit")
        run(["git", "commit", "-m", f"chore(release): v{new_version}"], 
            label="git commit", fatal=False)
    
    if do_tag:
        try:
            existing = subprocess.check_output(
                ["git", "tag", "-l", f"v{new_version}"], cwd=ROOT, text=True
            ).strip()
        except subprocess.CalledProcessError:
            existing = ""
        
        if existing:
            print(f"Tag v{new_version} already exists — skipping")
        else:
            run(["git", "tag", "-a", f"v{new_version}", "-m", f"v{new_version}"],
                label="git tag", fatal=False)

def dotnet_pack(csproj: Path, configuration: str, label: str):
    """Reliably pack a project by building first, then packing without building."""
    # Build first to ensure all referenced analyzer outputs exist
    run(["dotnet", "build", str(csproj), "-c", configuration])
    # Then pack without building again
    run(["dotnet", "pack", str(csproj), "-c", configuration, "--no-build", "-o", str(NUGET_DIR)],
        label=label)

def restore_tests_with_local():
    """Restore test projects"""
    for csproj in find_csprojs():
        if is_test_project(csproj):
            name = csproj.parent.name
            run(["dotnet", "restore", str(csproj),
                 "--force-evaluate",
                 "--source", str(NUGET_DIR),
                 "--source", "https://api.nuget.org/v3/index.json"],
                label=f"restore {name}")

def run_tests(configuration: str) -> List[Path]:
    """Run tests and return list of failed projects"""
    failed = []
    for csproj in find_csprojs():
        if is_test_project(csproj):
            name = csproj.parent.name
            rc = run(["dotnet", "test", str(csproj), "-c", configuration],
                    fatal=False, label=f"test {name}")
            if rc != 0:
                failed.append(csproj)
    return failed

def main():
    global _ui, _show_warnings
    
    ap = argparse.ArgumentParser(description="Release builder with advanced TUI")
    
    # Version options
    g = ap.add_mutually_exclusive_group()
    g.add_argument("--version", help="Set version, e.g. 0.8.0.18")
    g.add_argument("--bump", choices=["patch","minor","major"], 
                   help="Bump version from current")
    
    # Build options
    ap.add_argument("--configuration", default="Release")
    ap.add_argument("--no-commit", action="store_true")
    ap.add_argument("--no-tag", action="store_true")
    ap.add_argument("--no-tests", action="store_true")
    
    # Branch suffix options
    ap.add_argument("--no-branch-suffix", action="store_true",
                    help="Don't add branch as prerelease suffix")
    ap.add_argument("--branch", help="Force branch name")
    
    # TUI options
    ap.add_argument("--ui", choices=['auto', 'rich', 'ansi', 'plain'], default='auto',
                    help="UI mode (default: auto)")
    ap.add_argument("--progress", action="store_true",
                    help="Show progress percentage")
    ap.add_argument("--no-color", action="store_true",
                    help="Disable colors")
    ap.add_argument("--show-warnings", action="store_true",
                    help="Show all warnings")
    ap.add_argument("--plain", action="store_true",
                    help="Force plain output (same as --ui plain)")
    ap.add_argument("--loader", action="store_true",
                    help="Show demoscene loader at startup")
    
    args = ap.parse_args()
    
    # UI mode override
    if args.plain:
        args.ui = 'plain'
    
    # Show loader if requested
    if args.loader and args.ui != 'plain':
        from tui import show_loader
        import shutil
        # Get terminal size
        try:
            width, height = shutil.get_terminal_size((80, 24))
            height = min(40, height - 1)
            width = min(120, width)
        except:
            width, height = 80, 24
        show_loader(duration=3.0, width=width, height=height)
    
    # Initialize UI
    _show_warnings = args.show_warnings
    use_color = not args.no_color
    _ui = create_tui(mode=args.ui, use_progress=args.progress, use_color=use_color)
    
    # Sanity checks
    for key, (_, proj) in PACKAGE_IDS.items():
        if not proj.exists():
            print(f"Missing project: {proj}", file=sys.stderr)
            sys.exit(1)
    
    # Version determination
    current = parse_version_from_stamp(FASTFSM_PROJ)
    
    branch_name = args.branch.strip() if args.branch else get_current_branch()
    branch_label = sanitize_branch_for_prerelease(branch_name)
    use_branch_suffix = not args.no_branch_suffix
    
    if not use_branch_suffix:
        new_version = args.version.strip() if args.version else bump(current, args.bump or "patch")
    else:
        if args.version:
            base = args.version.strip()
            bparsed = parse_semver(base)
            if not bparsed:
                print(f"ERROR: Invalid version: {base}", file=sys.stderr)
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
    
    print(f"Branch: {branch_name} → suffix: {branch_label}")
    print(f"Version: {current} -> {new_version}")
    
    # Collect and register tasks
    tasks = collect_tasks(args)
    _ui.register(tasks)
    
    try:
        # Update versions
        set_version_in_stamp(FASTFSM_PROJ, new_version)
        set_version_in_stamp(LOGGING_PROJ, new_version)
        set_version_in_stamp(DI_PROJ, new_version)
        
        update_packageref(DI_PROJ, "FastFsm.Net", new_version)
        update_packageref(LOGGING_PROJ, "FastFsm.Net", new_version)
        
        update_tests_versions(new_version)
        
        # Git operations
        git_commit_and_tag(new_version, 
                          do_commit=not args.no_commit, 
                          do_tag=not args.no_tag)
        
        # Pack
        NUGET_DIR.mkdir(exist_ok=True)
        dotnet_pack(FASTFSM_PROJ, args.configuration, "pack FastFsm")
        dotnet_pack(DI_PROJ, args.configuration, "pack FastFsm.DependencyInjection")
        dotnet_pack(LOGGING_PROJ, args.configuration, "pack FastFsm.Logging")
        
        # Restore
        restore_tests_with_local()
        
        # Test
        failed_tests = []
        if not args.no_tests:
            failed_tests = run_tests(args.configuration)
        
        # Final summary
        _ui.summary()
        
        print(f"\nPackages in: {NUGET_DIR}")
        
        if failed_tests:
            print("\nTest failures:")
            for p in failed_tests:
                print(" -", p.relative_to(ROOT))
            sys.exit(1)
            
    except KeyboardInterrupt:
        print("\nBuild interrupted")
        sys.exit(130)
    finally:
        if _ui:
            _ui.close()

if __name__ == "__main__":
    main()
