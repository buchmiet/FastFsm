"""Rich-based TUI implementation"""
from .base import ITui
from typing import List, Optional
import time
import threading

try:
    from rich.live import Live
    from rich.table import Table
    from rich.console import Console
    from rich.progress import SpinnerColumn, BarColumn, TextColumn, TimeElapsedColumn
    from rich import box
    RICH_AVAILABLE = True
except ImportError:
    RICH_AVAILABLE = False

class RichTui(ITui):
    """Rich library based TUI with live updating table"""
    
    def __init__(self, use_progress=False, use_color=True):
        if not RICH_AVAILABLE:
            raise ImportError("Rich library not available")
        
        super().__init__()
        self.use_progress = use_progress
        self.use_color = use_color
        self.console = Console(color_system="auto" if use_color else None)
        self.live = None
        self.update_thread = None
        self.running = False
        self._lock = threading.Lock()
    
    def _get_status_icon(self, status):
        """Get status icon with color"""
        icons = {
            'pending': ('·', 'dim white'),
            'running': ('▶', 'cyan'),
            'done': ('✓', 'green'),
            'failed': ('✗', 'red')
        }
        icon, color = icons.get(status, ('?', 'white'))
        if self.use_color:
            return f"[{color}]{icon}[/{color}]"
        return icon
    
    def _format_time(self, seconds):
        """Format elapsed time"""
        if seconds < 60:
            return f"{seconds:.1f}s"
        minutes = int(seconds // 60)
        secs = seconds % 60
        return f"{minutes}:{secs:04.1f}"
    
    def _create_table(self):
        """Create the status table"""
        table = Table(box=box.SIMPLE, show_header=True)
        table.add_column("", width=2)  # Status icon
        table.add_column("Task", style="bright_white")
        table.add_column("Time", justify="right", width=8)
        table.add_column("W/E", justify="right", width=8)  # Warnings/Errors
        
        if self.use_progress:
            table.add_column("Progress", justify="right", width=10)
        
        for task in self.tasks:
            icon = self._get_status_icon(task.status)
            
            # Task name - dim if not started, bright if running
            if task.status == 'pending':
                task_style = "dim white"
            elif task.status == 'running':
                task_style = "bright_cyan"
            else:
                task_style = "white"
            
            # Time
            if task.status == 'running':
                time_str = self._format_time(time.time() - task.start_time)
            elif task.elapsed > 0:
                time_str = self._format_time(task.elapsed)
            else:
                time_str = ""
            
            # Warnings/Errors
            if task.warnings > 0 or task.errors > 0:
                we_str = f"{task.warnings}/{task.errors}"
                if task.errors > 0:
                    we_style = "red"
                elif task.warnings > 0:
                    we_style = "yellow"
                else:
                    we_style = "white"
                we_str = f"[{we_style}]{we_str}[/{we_style}]" if self.use_color else we_str
            else:
                we_str = ""
            
            # Progress
            row = [icon, f"[{task_style}]{task.label}[/{task_style}]", time_str, we_str]
            
            if self.use_progress:
                if task.status == 'running':
                    # Show percentage or spinner
                    if task.estimated_time > 0:
                        prog_str = f"{task.progress}%"
                    else:
                        # Simple spinner
                        spinner = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏']
                        idx = int(time.time() * 10) % len(spinner)
                        prog_str = spinner[idx]
                    row.append(f"[cyan]{prog_str}[/cyan]" if self.use_color else prog_str)
                else:
                    row.append("")
            
            table.add_row(*row)
        
        return table
    
    def _update_loop(self):
        """Background thread to update display"""
        while self.running:
            with self._lock:
                if self.live:
                    self.live.update(self._create_table())
            time.sleep(0.1)
    
    def register(self, task_labels: List[str]):
        """Register tasks and start live display"""
        super().register(task_labels)
        self.live = Live(self._create_table(), console=self.console, refresh_per_second=10)
        self.live.start()
        self.running = True
        self.update_thread = threading.Thread(target=self._update_loop, daemon=True)
        self.update_thread.start()
    
    def start(self, label: str):
        """Mark task as started"""
        with self._lock:
            super().start(label)
    
    def update(self, label: str, warnings: int, errors: int, progress: Optional[int] = None):
        """Update running task"""
        with self._lock:
            super().update(label, warnings, errors, progress)
    
    def finish(self, label: str, success: bool, warnings: int, errors: int):
        """Mark task as completed"""
        with self._lock:
            super().finish(label, success, warnings, errors)
    
    def complete(self, task_id: str, failed=False):
        """Complete a task (compatibility alias)."""
        # Find task by id/label
        for task in self.tasks:
            if getattr(task, 'id', task.label) == task_id:
                self.finish(task.label, success=not failed, 
                           warnings=task.warnings, errors=task.errors)
                break
    
    def summary(self):
        """Show final summary and cleanup"""
        self.running = False
        if self.update_thread:
            self.update_thread.join(timeout=1)
        
        if self.live:
            self.live.stop()
        
        # Print final summary
        total_time = time.time() - self.start_time
        done = sum(1 for t in self.tasks if t.status == 'done')
        failed = sum(1 for t in self.tasks if t.status == 'failed')
        
        print(f"\n✨ Build complete in {self._format_time(total_time)}")
        print(f"   Tasks: {done} done, {failed} failed")
        print(f"   Warnings: {self.total_warnings}, Errors: {self.total_errors}")
        
        super().summary()