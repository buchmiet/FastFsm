#!/usr/bin/env python3
"""Demo script to show Rich TUI in action"""

import sys
import time
sys.path.insert(0, '.')
from tui import create_tui

# Test different TUI modes
def demo_tui(mode='rich'):
    print(f"\n=== Testing {mode.upper()} TUI ===\n")
    
    ui = create_tui(mode=mode, use_progress=True, use_color=True)
    
    # Register tasks
    tasks = [
        "Initialize project",
        "Download dependencies", 
        "Compile sources",
        "Run unit tests",
        "Package artifacts",
        "Deploy to server"
    ]
    
    ui.register(tasks)
    
    # Simulate task execution
    for i, task in enumerate(tasks):
        ui.start(task)
        
        # Simulate work with progress updates
        for progress in range(0, 100, 20):
            time.sleep(0.3)  # Simulate work
            
            # Add some warnings/errors randomly
            warns = (i % 2) * 2  # Even tasks have warnings
            errs = 1 if i == 3 else 0  # Task 3 has an error
            
            ui.update(task, warns, errs, progress)
        
        # Finish task
        success = (i != 3)  # Task 3 fails
        ui.finish(task, success, warns, errs)
    
    # Show summary
    ui.summary()
    ui.close()
    
    print(f"\n=== {mode.upper()} TUI Demo Complete ===\n")

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="Demo TUI modes")
    parser.add_argument("--mode", choices=['rich', 'ansi', 'plain'], default='rich',
                       help="TUI mode to demonstrate")
    args = parser.parse_args()
    
    demo_tui(args.mode)