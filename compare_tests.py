#!/usr/bin/env python3
import json
from collections import defaultdict

# Load oldtests.ndjson
oldtests = {}
with open('oldtests.ndjson', 'r') as f:
    for line in f:
        test = json.loads(line)
        oldtests[test['nazwatestu']] = test

# Load newtests.ndjson
newtests = {}
with open('newtests.ndjson', 'r') as f:
    for line in f:
        test = json.loads(line)
        newtests[test['nazwatestu']] = test

# Find tests in newtests but not in oldtests
missing_in_old = []
for name, test in newtests.items():
    if name not in oldtests:
        missing_in_old.append(test)

# Find tests in oldtests but not in newtests
missing_in_new = []
for name, test in oldtests.items():
    if name not in newtests:
        missing_in_new.append(test)

# Find tests with same name (common tests)
common_tests = []
for name in newtests.keys():
    if name in oldtests:
        common_tests.append({
            'name': name,
            'old': oldtests[name],
            'new': newtests[name]
        })

print("=" * 80)
print("PORÓWNANIE TESTÓW: oldtests vs newtests")
print("=" * 80)
print(f"\nStatystyki:")
print(f"  Testy w oldtests.ndjson: {len(oldtests)}")
print(f"  Testy w newtests.ndjson: {len(newtests)}")
print(f"  Wspólne testy (te same nazwy): {len(common_tests)}")
print(f"  Testy tylko w newtests (brakujące w oldtests): {len(missing_in_old)}")
print(f"  Testy tylko w oldtests (usunięte z newtests): {len(missing_in_new)}")

if missing_in_old:
    print("\n" + "=" * 80)
    print("TESTY W NEWTESTS, KTÓRYCH BRAKUJE W OLDTESTS:")
    print("=" * 80)
    for test in sorted(missing_in_old, key=lambda x: x['sciezkadopliku']):
        print(f"\n✗ {test['nazwatestu']}")
        print(f"  Plik: {test['sciezkadopliku']}:{test['row']}")

if missing_in_new:
    print("\n" + "=" * 80)
    print("TESTY W OLDTESTS, KTÓRYCH BRAKUJE W NEWTESTS:")
    print("=" * 80)

    # Group by file for better readability
    by_file = defaultdict(list)
    for test in missing_in_new:
        by_file[test['sciezkadopliku']].append(test)

    for filepath in sorted(by_file.keys()):
        print(f"\n{filepath}:")
        for test in sorted(by_file[filepath], key=lambda x: x['row']):
            print(f"  • {test['nazwatestu']} (linia {test['row']})")

if len(missing_in_old) == 0:
    print("\n" + "=" * 80)
    print("✓ WSZYSTKIE TESTY Z NEWTESTS SĄ W OLDTESTS")
    print("=" * 80)

# Summary
print("\n" + "=" * 80)
print("PODSUMOWANIE:")
print("=" * 80)
if len(missing_in_old) == 0:
    print("✓ oldtests zawiera wszystkie testy z newtests")
else:
    print(f"✗ oldtests NIE zawiera {len(missing_in_old)} testów z newtests")

if len(missing_in_new) > 0:
    print(f"ℹ oldtests zawiera {len(missing_in_new)} dodatkowych testów, których nie ma w newtests")
