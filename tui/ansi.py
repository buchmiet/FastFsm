"""ANSI-based TUI implementation (fallback when Rich not available)"""
from .base import ITui
from typing import List, Optional
import time
import sys
import threading
import shutil

# ANSI escape codes
class ANSI:
    RESET = '\033[0m'
    BOLD = '\033[1m'
    DIM = '\033[2m'
    
    # Colors
    BLACK = '\033[30m'
    RED = '\033[31m'
    GREEN = '\033[32m'
    YELLOW = '\033[33m'
    BLUE = '\033[34m'
    MAGENTA = '\033[35m'
    CYAN = '\033[36m'
    WHITE = '\033[37m'
    GRAY = '\033[90m'
    
    # Cursor control
    CLEAR_LINE = '\033[2K'
    CURSOR_UP = '\033[A'
    CURSOR_HOME = '\033[H'
    SAVE_POS = '\033[s'
    RESTORE_POS = '\033[u'

class AnsiTui(ITui):
    """ANSI terminal codes based TUI"""
    
    def __init__(self, use_progress=False):
        super().__init__()
        self.use_progress = use_progress
        self.running = False
        self.update_thread = None
        self._lock = threading.Lock()
        self.last_lines = 0
    
    def _get_status(self, task):
        """Get status character and color"""
        if task.status == 'pending':
            return ANSI.GRAY + '·' + ANSI.RESET
        elif task.status == 'running':
            return ANSI.CYAN + '▶' + ANSI.RESET
        elif task.status == 'done':
            return ANSI.GREEN + '✓' + ANSI.RESET
        elif task.status == 'failed':
            return ANSI.RED + '✗' + ANSI.RESET
        return '?'
    
    def _format_time(self, seconds):
        """Format elapsed time"""
        if seconds < 60:
            return f"{seconds:5.1f}s"
        minutes = int(seconds // 60)
        secs = seconds % 60
        return f"{minutes:2d}:{secs:04.1f}"
    
    def _render(self):
        """Render the task list"""
        lines = []
        term_width = shutil.get_terminal_size((80, 20)).columns
        
        # Header
        header = f"{'='*term_width}"
        lines.append(ANSI.DIM + header + ANSI.RESET)
        
        # Tasks
        for task in self.tasks:
            status = self._get_status(task)
            
            # Task label with color
            if task.status == 'pending':
                label = ANSI.GRAY + task.label + ANSI.RESET
            elif task.status == 'running':
                label = ANSI.CYAN + ANSI.BOLD + task.label + ANSI.RESET
            else:
                label = task.label
            
            # Time
            if task.status == 'running' and task.start_time:
                time_str = self._format_time(time.time() - task.start_time)
            elif task.elapsed > 0:
                time_str = self._format_time(task.elapsed)
            else:
                time_str = "     "
            
            # Warnings/Errors
            we_str = ""
            if task.warnings > 0:
                we_str += ANSI.YELLOW + f"W:{task.warnings}" + ANSI.RESET + " "
            if task.errors > 0:
                we_str += ANSI.RED + f"E:{task.errors}" + ANSI.RESET + " "
            
            # Progress (if enabled and running)
            progress_str = ""
            if self.use_progress and task.status == 'running':
                if task.progress > 0:
                    progress_str = f" [{task.progress:3d}%]"
                else:
                    # Simple spinner
                    spinner = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏']
                    idx = int(time.time() * 10) % len(spinner)
                    progress_str = f" {spinner[idx]}"
            
            # Combine line
            line = f" {status} {label:40s} {time_str:7s} {we_str:15s}{progress_str}"
            
            # Truncate if too long
            if len(line) > term_width:
                line = line[:term_width-1]
            
            lines.append(line)
        
        # Footer with totals
        elapsed = time.time() - self.start_time
        footer = f" Total: {self._format_time(elapsed)} | W:{self.total_warnings} E:{self.total_errors}"
        lines.append(ANSI.DIM + footer + ANSI.RESET)
        
        return lines
    
    def _clear_previous(self):
        """Clear previous output"""
        if self.last_lines > 0:
            # Move cursor up and clear lines
            for _ in range(self.last_lines):
                sys.stdout.write(ANSI.CURSOR_UP + ANSI.CLEAR_LINE)
            sys.stdout.flush()
    
    def _update_display(self):
        """Update the display"""
        with self._lock:
            lines = self._render()
            self._clear_previous()
            for line in lines:
                print(line)
            sys.stdout.flush()
            self.last_lines = len(lines)
    
    def _update_loop(self):
        """Background thread to update display"""
        while self.running:
            self._update_display()
            time.sleep(0.1)
    
    def register(self, task_labels: List[str]):
        """Register tasks and start display"""
        super().register(task_labels)
        self.running = True
        self._update_display()
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
    
    def summary(self):
        """Show final summary"""
        self.running = False
        if self.update_thread:
            self.update_thread.join(timeout=0.5)
        
        # Clear the display one last time and show final state
        self._clear_previous()
        
        # Print final summary
        total_time = time.time() - self.start_time
        done = sum(1 for t in self.tasks if t.status == 'done')
        failed = sum(1 for t in self.tasks if t.status == 'failed')
        
        print(f"\n{ANSI.GREEN}✨ Build complete in {self._format_time(total_time)}{ANSI.RESET}")
        print(f"   Tasks: {done} done, {failed} failed")
        print(f"   Warnings: {self.total_warnings}, Errors: {self.total_errors}")
        
        super().summary()