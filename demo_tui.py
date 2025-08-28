#!/usr/bin/env python3
"""
Demo script for enhanced TUI with parallel rendering.
"""

import sys
import time
import threading
import random

# Add TUI module to path
sys.path.insert(0, '.')

from tui.richui_v2 import RichTuiV2, RICH_AVAILABLE
from tui.base import TaskInfo

def simulate_task(ui, task_id, duration=3.0):
    """Simulate a task with warnings/errors."""
    ui.start(task_id)
    
    steps = 20
    for i in range(steps):
        time.sleep(duration / steps)
        
        # Random warnings/errors
        if random.random() < 0.1:
            ui.update(task_id, warning=True)
        if random.random() < 0.05:
            ui.update(task_id, error=True)
        
        # Update progress
        ui.update(task_id, line=f"Processing step {i+1}/{steps}")
    
    # Complete with random success
    success = random.random() > 0.2
    ui.complete(task_id, failed=not success)

def main():
    """Run the demo."""
    if not RICH_AVAILABLE:
        print("Rich library not available. Install with: pip install rich")
        return
    
    # Create UI
    ui = RichTuiV2(use_progress=True, use_color=True)
    ui.set_metadata('develop', 'v0.6.2.21-develop')
    
    # Create tasks
    tasks = []
    task_names = [
        ('pack FastFsm', 2.0),
        ('pack FastFsm.DependencyInjection', 2.5),
        ('pack FastFsm.Logging', 2.0),
        ('restore FastFsm.Tests', 3.0),
        ('restore FastFsm.DependencyInjection.Tests', 3.5),
        ('restore FastFsm.Logging.Tests', 3.0),
        ('test FastFsm.Tests', 5.0),
        ('test FastFsm.DependencyInjection.Tests', 4.0),
        ('test FastFsm.Logging.Tests', 4.5),
    ]
    
    for name, duration in task_names:
        task = TaskInfo(name, estimated_time=duration)
        task.id = name.replace(' ', '_').replace('.', '_')
        task.name = name
        tasks.append(task)
    
    ui.register(tasks)
    
    # Start UI thread
    ui_thread = threading.Thread(target=ui.run)
    ui_thread.daemon = True
    ui_thread.start()
    
    # Simulate tasks in parallel
    threads = []
    for task in tasks[:3]:  # Start first 3 in parallel
        t = threading.Thread(
            target=simulate_task,
            args=(ui, task.id, task.estimated_time)
        )
        t.start()
        threads.append(t)
    
    # Wait for first batch
    for t in threads:
        t.join()
    
    # Start next batch
    threads = []
    for task in tasks[3:6]:  # Next 3 in parallel
        t = threading.Thread(
            target=simulate_task,
            args=(ui, task.id, task.estimated_time)
        )
        t.start()
        threads.append(t)
    
    for t in threads:
        t.join()
    
    # Final batch
    threads = []
    for task in tasks[6:]:  # Last tasks
        t = threading.Thread(
            target=simulate_task,
            args=(ui, task.id, task.estimated_time)
        )
        t.start()
        threads.append(t)
    
    for t in threads:
        t.join()
    
    # Let UI finish
    time.sleep(1)
    ui.stop()
    ui_thread.join(timeout=2)
    
    # Summary
    ui.summary()

if __name__ == "__main__":
    main()
