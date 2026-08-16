#!/usr/bin/env python3
import json

# Load oldtests.ndjson
oldtests_names = set()
with open('oldtests.ndjson', 'r') as f:
    for line in f:
        test = json.loads(line)
        oldtests_names.add(test['nazwatestu'])

# Load newtests.ndjson
newtests = []
with open('newtests.ndjson', 'r') as f:
    for line in f:
        test = json.loads(line)
        newtests.append(test)

# Find tests in newtests that don't have exact match in oldtests
missing = []
for test in newtests:
    newtest_name = test['nazwatestu']

    # Check exact match
    if newtest_name not in oldtests_names:
        # Check if there's a version without "Legacy_" prefix
        if newtest_name.startswith('Legacy_'):
            name_without_legacy = newtest_name.replace('Legacy_', '', 1)
            has_equivalent = name_without_legacy in oldtests_names
        else:
            has_equivalent = False

        missing.append({
            'name': newtest_name,
            'file': test['sciezkadopliku'],
            'row': test['row'],
            'has_equivalent': has_equivalent
        })

print("TESTY Z NEWTESTS BEZ ODPOWIEDNIKA W OLDTESTS:")
print("=" * 80)

truly_missing = [t for t in missing if not t['has_equivalent']]
has_legacy_equivalent = [t for t in missing if t['has_equivalent']]

if truly_missing:
    print("\nTesty bez żadnego odpowiednika:")
    for test in truly_missing:
        print(f"\n✗ {test['name']}")
        print(f"  Plik: {test['file']}:{test['row']}")
else:
    print("\n✓ Wszystkie testy z newtests mają swoje odpowiedniki w oldtests")

if has_legacy_equivalent:
    print(f"\n\nTesty z prefiksem 'Legacy_' ({len(has_legacy_equivalent)} testów):")
    print("(mają odpowiedniki w oldtests bez prefiksu 'Legacy_')")
    for test in has_legacy_equivalent:
        equivalent_name = test['name'].replace('Legacy_', '', 1)
        print(f"\n  {test['name']}")
        print(f"    → odpowiednik w oldtests: {equivalent_name}")

print("\n" + "=" * 80)
print(f"PODSUMOWANIE: {len(truly_missing)} testów bez odpowiednika")
