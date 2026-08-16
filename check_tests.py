#!/usr/bin/env python3
import json
import re
import os
from pathlib import Path

# Load oldtests.ndjson
tests_in_ndjson = set()
with open('oldtests.ndjson', 'r') as f:
    for line in f:
        test = json.loads(line)
        tests_in_ndjson.add((test['sciezkadopliku'], test['nazwatestu']))

# Find all [Fact] attributes in .cs files
def find_tests_with_fact():
    test_files = []
    for root, dirs, files in os.walk('oldtests'):
        # Skip obj directories
        dirs[:] = [d for d in dirs if d != 'obj']
        for file in files:
            if file.endswith('.cs'):
                test_files.append(os.path.join(root, file))

    results = []
    for filepath in test_files:
        with open(filepath, 'r', encoding='utf-8') as f:
            lines = f.readlines()

        for i, line in enumerate(lines):
            # Check if line contains [Fact]
            if re.search(r'^\s*\[Fact\]', line):
                # Look at the next line for the test method name
                if i + 1 < len(lines):
                    next_line = lines[i + 1]
                    # Match method name pattern
                    method_match = re.search(r'\s+(public|private|internal|protected)?\s*(async\s+)?\w+\s+(\w+)\s*\(', next_line)
                    if method_match:
                        method_name = method_match.group(3)
                        results.append({
                            'file': filepath,
                            'line': i + 1,  # [Fact] line number (1-indexed)
                            'method': method_name,
                            'method_line': i + 2  # Method declaration line (1-indexed)
                        })

    return results

# Find all [Fact] tests
fact_tests = find_tests_with_fact()

# Check which tests are missing
missing_tests = []
for test in fact_tests:
    if (test['file'], test['method']) not in tests_in_ndjson:
        missing_tests.append(test)

# Print results
print(f"Total [Fact] tests found: {len(fact_tests)}")
print(f"Tests in oldtests.ndjson: {len(tests_in_ndjson)}")
print(f"Missing tests: {len(missing_tests)}")
print()

if missing_tests:
    print("Tests with [Fact] NOT found in oldtests.ndjson:")
    print("=" * 80)
    for test in missing_tests:
        print(f"File: {test['file']}")
        print(f"  [Fact] at line: {test['line']}")
        print(f"  Method: {test['method']} (line {test['method_line']})")
        print()
else:
    print("✓ All [Fact] tests are present in oldtests.ndjson")
