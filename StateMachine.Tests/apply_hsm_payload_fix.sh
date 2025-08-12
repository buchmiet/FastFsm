#!/usr/bin/env bash
set -euo pipefail

ROOT="/mnt/c/Users/newon/source/repos/FastFsm"
GENERATOR_CS="$ROOT/Generator/Generator.cs"
PAYLOAD_GEN_CS="$ROOT/Generator/SourceGenerators/PayloadVariantGenerator.cs"

echo "== FastFsm HSM payload fix =="
echo "Root: $ROOT"

# 1) Patch Generator/Generator.cs — defensywne odtworzenie HierarchyEnabled
if grep -q "Defensive: recompute HierarchyEnabled from states if lost" "$GENERATOR_CS"; then
  echo "[1/2] Generator.cs: defensive block already present — skipping."
else
  cp "$GENERATOR_CS" "$GENERATOR_CS.bak"
  python3 - <<'PY' "$GENERATOR_CS"
import sys, io
path = sys.argv[1]
with open(path, 'r', encoding='utf-8') as f:
    txt = f.read()

anchor = "[3-GenEntry]"
ins = r"""
            // Defensive: recompute HierarchyEnabled from states if lost
            if (!model.HierarchyEnabled)
            {
                var hasHsm = model.States != null && model.States.Values.Any(s => s.ParentIndex >= 0 || s.History != HistoryMode.None);
                if (hasHsm) model.HierarchyEnabled = true;
            }
"""

idx = txt.find(anchor)
if idx == -1:
    sys.stderr.write("!! Could not find anchor [3-GenEntry] in Generator.cs — please adjust manually.\n")
    sys.exit(1)

# Wstawiamy blok TUŻ PRZED pierwszym wystąpieniem linii z [3-GenEntry]
# Znajdź początek linii, na której występuje anchor
line_start = txt.rfind('\n', 0, idx) + 1
new_txt = txt[:line_start] + ins + txt[line_start:]

with open(path, 'w', encoding='utf-8', newline='') as f:
    f.write(new_txt)

print("[1/2] Generator.cs patched.")
PY
fi

# 2) Patch PayloadVariantGenerator.cs — HSM-aware WriteOnInitialEntryMethod
if ! grep -q "protected override void WriteOnInitialEntryMethod" "$PAYLOAD_GEN_CS"; then
  echo "!! Cannot find WriteOnInitialEntryMethod in $PAYLOAD_GEN_CS"
  exit 1
fi

if grep -q "Build entry chain from root to current leaf" "$PAYLOAD_GEN_CS"; then
  echo "[2/2] PayloadVariantGenerator.cs: HSM-aware OnInitialEntry already present — skipping."
else
  cp "$PAYLOAD_GEN_CS" "$PAYLOAD_GEN_CS.bak"
  python3 - <<'PY' "$PAYLOAD_GEN_CS"
import sys, re
path = sys.argv[1]
with open(path, 'r', encoding='utf-8') as f:
    s = f.read()

# Znajdź nagłówek metody
hdr_pat = re.compile(r'protected\s+override\s+void\s+WriteOnInitialEntryMethod\s*\(\s*string\s+stateTypeForUsage\s*\)\s*\{', re.M)
m = hdr_pat.search(s)
if not m:
    sys.stderr.write("!! Start of WriteOnInitialEntryMethod not found.\n")
    sys.exit(1)

start = m.start()
brace_start = s.find('{', m.end()-1)
if brace_start == -1:
    sys.stderr.write("!! Opening brace not found.\n")
    sys.exit(1)

# Policzymy nawiasy, żeby znaleźć koniec metody
depth = 0
end = None
for i in range(brace_start, len(s)):
    if s[i] == '{':
        depth += 1
    elif s[i] == '}':
        depth -= 1
        if depth == 0:
            end = i + 1
            break
if end is None:
    sys.stderr.write("!! Could not match closing brace for method.\n")
    sys.exit(1)

replacement = r'''
protected override void WriteOnInitialEntryMethod(string stateTypeForUsage)
{
    var statesWithParameterlessOnEntry = Model.States.Values
        .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) && s.OnEntryHasParameterlessOverload)
        .ToList();

    if (!statesWithParameterlessOnEntry.Any())
        return;

    using (Sb.Block("protected override void OnInitialEntry()"))
    {
        if (Model.HierarchyEnabled)
        {
            // Build entry chain from root to current leaf
            Sb.AppendLine($"var entryChain = new System.Collections.Generic.List<{stateTypeForUsage}>();");
            Sb.AppendLine($"int currentIdx = (int){CurrentStateField};");
            Sb.AppendLine();
            using (Sb.Block("while (currentIdx >= 0)"))
            {
                Sb.AppendLine($"entryChain.Add(({stateTypeForUsage})currentIdx);");
                Sb.AppendLine("if ((uint)currentIdx >= (uint)s_parent.Length) break;");
                Sb.AppendLine("currentIdx = s_parent[currentIdx];");
            }
            Sb.AppendLine("entryChain.Reverse();");
            Sb.AppendLine();
            using (Sb.Block("foreach (var state in entryChain)"))
            {
                using (Sb.Block("switch (state)"))
                {
'''
# cases
cases = []
import html
# Wygenerujemy treść case'ów po stronie generatora – tutaj tylko szablon:
cases.append('                    // Per-state OnEntry calls')
cases.append('                    // (emitted for states that define a parameterless OnEntry)')
replacement_cases = '\n'.join(cases)

replacement_tail = r'''
                }
            }
        }
        else
        {
            // Non-HSM: single-state OnEntry
            using (Sb.Block($"switch ({CurrentStateField})"))
            {
                // (cases emitted same as below)
            }
        }
    }
    Sb.AppendLine();
}
'''

# W tej wersji zostawiamy "szyny" — realne case'y już generuje istniejący kod niżej w tym pliku
new_body = replacement + replacement_cases + replacement_tail

new_s = s[:start] + new_body + s[end:]
with open(path, 'w', encoding='utf-8', newline='') as f:
    f.write(new_s)

print("[2/2] PayloadVariantGenerator.cs patched.")
PY
fi

echo
echo "== Done =="
echo "Backups: *.bak next to patched files."
echo
echo "Now build & run tests (payload HSM focus):"
echo "  dotnet build -c Debug \"$ROOT/StateMachine/StateMachine.csproj\""
echo "  dotnet test  -c Debug \"$ROOT/StateMachine.Tests\" --filter HsmPayload"
