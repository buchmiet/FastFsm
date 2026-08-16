#!/usr/bin/env python3
"""
Split FastFsm.Async.Tests into two standalone test projects:
 - FastFsm.Async.Legacy.Tests: runs legacy (attribute-based) machines
 - FastFsm.Async.Fluent.Tests: runs fluent (DSL) machines

This tool copies sources from FastFsm.Async.Tests and rewrites test code:
 - In Fluent project copies, replace object creations like `new FooMachine(`
   to `new FooMachineFluentFsm(` where a Fluent class exists.
 - In Legacy project copies, replace `new FooMachineFluentFsm(` to
   `new FooMachine(`.

It skips the Parity (matrix) infrastructure folder.

Run from repository root:
  python3 tools/split_async_tests.py
"""

from __future__ import annotations
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "FastFsm.Async.Tests"
FLUENT_DST = ROOT / "FastFsm.Async.Fluent.Tests"
LEGACY_DST = ROOT / "FastFsm.Async.Legacy.Tests"


def collect_files() -> list[Path]:
    files: list[Path] = []
    for p in (SRC / "Features").rglob("*.cs"):
        # Skip parity harness (matrix/wrappers)
        if "/Parity/" in str(p.as_posix()):
            continue
        files.append(p)
    # Top-level helpers that are safe to reuse
    files += [SRC / "ExceptionAsyncMachine.cs", SRC / "Dsl.cs"]
    return files


FLUENT_CLASS_RE = re.compile(r"class\s+(?P<name>[A-Za-z0-9_]+)FluentFsm\b")


def find_fluent_pairs(text: str) -> set[str]:
    # Returns base names that have a FooFluentFsm class in the file
    return {m.group("name") for m in FLUENT_CLASS_RE.finditer(text)}


def rewrite_to_fluent(text: str) -> str:
    # For each base Foo found in file (with FooFluentFsm present), replace
    # `new Foo(` with `new FooFluentFsm(` in the test code.
    bases = find_fluent_pairs(text)
    if not bases:
        return text
    for base in sorted(bases, key=len, reverse=True):
        text = re.sub(rf"new\s+{re.escape(base)}\s*\(", f"new {base}FluentFsm(", text)
    return text


def rewrite_to_legacy(text: str) -> str:
    # Replace new FooFluentFsm( → new Foo(
    text = re.sub(r"new\s+([A-Za-z0-9_]+)FluentFsm\s*\(", r"new \1(", text)
    return text


def copy_and_rewrite(dst: Path, mode: str) -> None:
    assert mode in {"fluent", "legacy"}
    for src in collect_files():
        rel = src.relative_to(SRC)
        out = dst / rel
        out.parent.mkdir(parents=True, exist_ok=True)
        data = src.read_text(encoding="utf-8")
        if mode == "fluent":
            data = rewrite_to_fluent(data)
        else:
            data = rewrite_to_legacy(data)
        out.write_text(data, encoding="utf-8")


def main() -> None:
    print("Preparing split copies of Async tests...")
    # Ensure directories exist
    (FLUENT_DST / "Features").mkdir(parents=True, exist_ok=True)
    (LEGACY_DST / "Features").mkdir(parents=True, exist_ok=True)

    copy_and_rewrite(FLUENT_DST, "fluent")
    copy_and_rewrite(LEGACY_DST, "legacy")

    print("Done. Sources were copied to:")
    print(f" - {FLUENT_DST}")
    print(f" - {LEGACY_DST}")


if __name__ == "__main__":
    main()

