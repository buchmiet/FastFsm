"""Plain text TUI implementation (no colors, no live updates)"""
from .base import ITui
from typing import List, Optional
import time

class PlainTui(ITui):
    """Plain text output - for CI/non-TTY environments"""
    
    def __init__(self):
        super().__init__()
        self.show_warnings = True  # In plain mode, show all warnings
    
    def register(self, task_labels: List[str]):
        """Register tasks"""
        super().register(task_labels)
        print(f"Build tasks: {len(task_labels)} registered")
    
    def start(self, label: str):
        """Mark task as started"""
        super().start(label)
        print(f">> {label}")
    
    def update(self, label: str, warnings: int, errors: int, progress: Optional[int] = None):
        """Update running task - no-op in plain mode"""
        super().update(label, warnings, errors, progress)
    
    def finish(self, label: str, success: bool, warnings: int, errors: int):
        """Mark task as completed"""
        super().finish(label, success, warnings, errors)
        
        status = "OK" if success else "FAILED"
        msg = f"   {status}"
        
        if warnings > 0 or errors > 0:
            msg += f" (warnings: {warnings}, errors: {errors})"
        
        if not success:
            print(f"   ERROR: {label} failed")
        elif warnings > 0 or errors > 0:
            print(msg)
    
    def summary(self):
        """Show final summary"""
        total_time = time.time() - self.start_time
        done = sum(1 for t in self.tasks if t.status == 'done')
        failed = sum(1 for t in self.tasks if t.status == 'failed')
        
        print(f"\nBuild complete in {total_time:.1f}s")
        print(f"Tasks: {done} done, {failed} failed")
        print(f"Total warnings: {self.total_warnings}, Total errors: {self.total_errors}")
        
        if failed > 0:
            print("\nFailed tasks:")
            for task in self.tasks:
                if task.status == 'failed':
                    print(f"  - {task.label}")
        
        super().summary()