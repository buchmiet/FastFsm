#!/usr/bin/env python3
import re
import sys

def remove_redundant_logging_checks(filepath):
    """
    Removes redundant if (ShouldGenerateLogging) checks that wrap WriteLogStatement calls.
    The WriteLogStatement method already checks ShouldGenerateLogging internally.
    """
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    # Pattern to match simple if blocks with WriteLogStatement
    # Handles both single-line and multi-line WriteLogStatement calls
    pattern1 = re.compile(
        r'(\s*)if\s*\(\s*ShouldGenerateLogging\s*\)\s*\n'
        r'\1\{\s*\n'
        r'(\1\s+WriteLogStatement\([^;]+;)\s*\n'
        r'\1\}',
        re.MULTILINE
    )
    
    # Replace with just the WriteLogStatement call (without the if wrapper)
    content = pattern1.sub(r'\2', content)
    
    # Pattern for multi-line WriteLogStatement calls
    pattern2 = re.compile(
        r'(\s*)if\s*\(\s*ShouldGenerateLogging\s*\)\s*\n'
        r'\1\{\s*\n'
        r'((?:\1\s+.*\n)+?\1\s+.*WriteLogStatement[^;]+;)\s*\n'
        r'\1\}',
        re.MULTILINE
    )
    
    content = pattern2.sub(r'\2', content)
    
    # Count replacements
    replacements = len(re.findall(r'if\s*\(\s*ShouldGenerateLogging\s*\)', original_content)) - \
                   len(re.findall(r'if\s*\(\s*ShouldGenerateLogging\s*\)', content))
    
    if content != original_content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Modified {filepath}: Removed {replacements} redundant ShouldGenerateLogging checks")
        return True
    else:
        print(f"No changes needed in {filepath}")
        return False

if __name__ == "__main__":
    files = [
        "Generator/SourceGenerators/StateMachineCodeGenerator.cs",
        "Generator/SourceGenerators/UnifiedStateMachineGenerator.cs"
    ]
    
    total_modified = 0
    for filepath in files:
        try:
            if remove_redundant_logging_checks(filepath):
                total_modified += 1
        except Exception as e:
            print(f"Error processing {filepath}: {e}")
    
    print(f"\nTotal files modified: {total_modified}")