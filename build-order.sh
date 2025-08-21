#!/usr/bin/env bash
set -euo pipefail

SLN="${1:-$(ls *.sln 2>/dev/null | head -n1)}"
if [[ -z "${SLN:-}" ]]; then
  echo "Brak pliku .sln (podaj ścieżkę jako argument)."
  exit 1
fi

dotnet restore "$SLN" >/dev/null 2>&1 || true

( dotnet msbuild "$SLN" -t:Build -m:1 -v:diag /nologo 2>&1 || true ) \
  | grep -E 'Project ".*\.csproj" (on node [0-9]+ )?\(Build target\(s\)\)' \
  | sed -n -E 's/^.*Project "([^"]+\.csproj)".*\(Build target\(s\)\).*$/\1/p' \
  | awk '!seen[$0]++' \
  | sed -E 's#.*/##; s#\.csproj$##' \
  | nl -w2 -s'. '
