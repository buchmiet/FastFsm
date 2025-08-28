#!/usr/bin/env python3
"""
Test enhanced TUI with clean FX banner.
"""

import sys
import time
import threading

sys.path.insert(0, '.')

from tui.richui_v2 import RichTuiV2, RICH_AVAILABLE
from tui.base import TaskInfo

def main():
    if not RICH_AVAILABLE:
        print("Rich not available")
        return
    
    print("Starting enhanced TUI test...")
    
    # Create UI
    ui = RichTuiV2()
    ui.set_metadata('develop', 'v0.6.2.22')
    
    # Create simple test tasks
    tasks = []
    for i in range(3):
        task = TaskInfo(f'Task {i+1}', estimated_time=2.0)
        task.id = f'task_{i}'
        task.name = f'Task {i+1}'
        tasks.append(task)
    
    ui.register(tasks)
    
    # Start UI in thread
    ui_thread = threading.Thread(target=ui.run)
    ui_thread.daemon = True
    ui_thread.start()
    
    # Simulate task execution
    for i in range(3):
        ui.start(f'task_{i}')
        time.sleep(1)
        ui.update(f'task_{i}', warning=True)
        time.sleep(1)
        ui.complete(f'task_{i}')
    
    # Let UI finish
    time.sleep(2)
    ui.stop()
    ui_thread.join(timeout=2)
    
    print("\nTest completed!")

if __name__ == '__main__':
    main()