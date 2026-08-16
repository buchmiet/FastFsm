#!/usr/bin/env python3
"""
Iteratively run tests one-by-one to find hanging tests.

Usage examples:
  python3 tools/find_hanging_tests.py \
      --project FastFsm.Async.Tests/FastFsm.Async.Tests.csproj \
      --timeout 60

  # Run only tests containing substring 'Concurrency'
  python3 tools/find_hanging_tests.py -p FastFsm.Async.Tests/FastFsm.Async.Tests.csproj \
      --timeout 45 --contains Concurrency

Notes:
 - Uses `dotnet test --list-tests` to discover tests, then runs each with
   `--filter FullyQualifiedName=<test>`.
 - Wraps each invocation with the `timeout` shell (Linux) to ensure the whole
   test process is killed if it exceeds the per-test timeout.
 - Overrides project-level runsettings by passing a temporary minimal
   .runsettings file via `--settings` so that session-level timeouts from the
   project do not interfere with per-test diagnosis.
"""

import argparse
import os
import re
import shlex
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import List, Tuple


def run_cmd(cmd: List[str], cwd: Path | None = None) -> Tuple[int, str]:
    proc = subprocess.Popen(cmd, cwd=str(cwd) if cwd else None,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            text=True)
    out_lines: List[str] = []
    assert proc.stdout is not None
    for line in proc.stdout:
        out_lines.append(line)
    rc = proc.wait()
    return rc, "".join(out_lines)


def discover_tests(project: Path) -> List[str]:
    rc, out = run_cmd(["dotnet", "test", str(project), "--no-build", "--list-tests", "-v", "minimal"])
    if rc != 0:
        print("ERROR: failed to list tests. Output:\n" + out, file=sys.stderr)
        sys.exit(rc)

    tests: List[str] = []
    for raw in out.splitlines():
        line = raw.strip()
        # Heuristics: fully-qualified test names typically contain dots and no spaces
        if not line or line.startswith("[") or line.startswith("Test run for "):
            continue
        if line.startswith("The following Tests are available"):
            continue
        if line.startswith("Starting test execution"):
            continue
        if " -> " in line or line.endswith(".dll"):
            continue
        if " " in line:
            # Likely a display name; skip to keep FQN only
            continue
        if "." not in line:
            continue
        tests.append(line)

    if not tests:
        print("WARNING: No tests discovered via heuristics. Raw output:\n" + out)
    return tests


def build_minimal_runsettings() -> Path:
    content = """<?xml version=\"1.0\" encoding=\"utf-8\"?>
<RunSettings>
  <RunConfiguration>
    <ResultsDirectory>TestResults</ResultsDirectory>
    <!-- No TestSessionTimeout here: per-test timeout is enforced by the shell -->
    <MaxCpuCount>1</MaxCpuCount>
  </RunConfiguration>
</RunSettings>
"""
    fd, path = tempfile.mkstemp(prefix="iter-tests-", suffix=".runsettings")
    with os.fdopen(fd, "w", encoding="utf-8") as f:
        f.write(content)
    return Path(path)


def main() -> None:
    ap = argparse.ArgumentParser(description="Run tests one-by-one to find hangers")
    ap.add_argument("--project", "-p", default="FastFsm.Async.Tests/FastFsm.Async.Tests.csproj",
                    help="Path to the .csproj (default: FastFsm.Async.Tests)")
    ap.add_argument("--timeout", type=int, default=60,
                    help="Per-test timeout in seconds (default: 60)")
    ap.add_argument("--contains", default=None,
                    help="Only run tests whose FQN contains this substring")
    ap.add_argument("--start-from", default=None,
                    help="Skip until this exact FQN is found, then start running")
    ap.add_argument("--dotnet-args", default="",
                    help="Extra arguments passed to 'dotnet test' (quoted)")
    args = ap.parse_args()

    project = Path(args.project).resolve()
    if not project.exists():
        print(f"ERROR: project not found: {project}", file=sys.stderr)
        sys.exit(2)

    tests = discover_tests(project)
    if args.contains:
        tests = [t for t in tests if args.contains in t]

    if not tests:
        print("No tests to run after filtering")
        return

    if args.start_from and args.start_from in tests:
        idx = tests.index(args.start_from)
        tests = tests[idx:]

    settings = build_minimal_runsettings()
    extra = shlex.split(args.dotnet_args) if args.dotnet_args else []

    print(f"Discovered {len(tests)} tests. Per-test timeout: {args.timeout}s\n")

    results = []  # (name, status, duration_sec)
    for i, test in enumerate(tests, 1):
        print(f"[{i}/{len(tests)}] {test}")
        cmd = [
            "timeout", f"{args.timeout}s",
            "dotnet", "test", str(project), "--no-build", "-v", "minimal",
            "--settings", str(settings),
            "--filter", f"FullyQualifiedName={test}"
        ] + extra

        try:
            rc, out = run_cmd(cmd)
        except KeyboardInterrupt:
            print("Interrupted by user")
            break

        if rc == 0:
            status = "PASS"
        elif rc == 124:
            status = "HANG"
        else:
            # dotnet test returns non-zero on failures
            status = f"FAIL(rc={rc})"

        print(f" -> {status}")
        # Print last few lines for context when not PASS
        if status != "PASS":
            tail = "\n".join(out.splitlines()[-20:])
            print(tail)

        results.append((test, status))

    print("\nSummary:")
    for name, status in results:
        print(f" - {status:6s} {name}")

    # Non-zero exit if any hang/fail detected
    if any(s != "PASS" for _, s in results):
        sys.exit(1)


if __name__ == "__main__":
    main()

