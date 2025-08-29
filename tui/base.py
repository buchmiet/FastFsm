"""Base TUI interface"""
from abc import ABC, abstractmethod
from typing import List, Optional, Tuple
import time
import json
from pathlib import Path

class TaskInfo:
    """Information about a build task"""
    def __init__(self, label: str, estimated_time: float = 0.0):
        self.label = label
        self.estimated_time = estimated_time
        self.status = 'pending'  # pending, running, done, failed
        self.start_time = None
        self.elapsed = 0.0
        self.warnings = 0
        self.errors = 0
        self.progress = 0  # 0-100
    
    def __str__(self) -> str:
        """String representation for plain/ansi UI"""
        return getattr(self, "label", getattr(self, "name", ""))

class ITui(ABC):
    """Abstract base for TUI implementations"""
    
    def __init__(self):
        self.tasks = []
        self.current_task = None
        self.total_warnings = 0
        self.total_errors = 0
        self.start_time = time.time()
        self.times_file = Path.home() / '.build_times.json'
        self.estimated_times = self._load_times()
    
    def _load_times(self) -> dict:
        """Load historical build times for EMA calculation"""
        if self.times_file.exists():
            try:
                with open(self.times_file, 'r') as f:
                    return json.load(f)
            except:
                pass
        return {}
    
    def _save_times(self):
        """Save build times with EMA update"""
        try:
            with open(self.times_file, 'w') as f:
                json.dump(self.estimated_times, f)
        except:
            pass
    
    def _update_ema(self, label: str, elapsed: float):
        """Update exponential moving average for task time"""
        alpha = 0.3
        if label in self.estimated_times:
            old = self.estimated_times[label]
            self.estimated_times[label] = (1 - alpha) * old + alpha * elapsed
        else:
            self.estimated_times[label] = elapsed
    
    @abstractmethod
    def register(self, task_labels: List[str]):
        """Register all tasks that will be executed"""
        self.tasks = [TaskInfo(label, self.estimated_times.get(label, 5.0)) 
                      for label in task_labels]
    
    @abstractmethod
    def start(self, label: str):
        """Mark task as started"""
        for task in self.tasks:
            if task.label == label:
                task.status = 'running'
                task.start_time = time.time()
                self.current_task = task
                break
    
    @abstractmethod
    def update(self, label: str, warnings: int, errors: int, progress: Optional[int] = None):
        """Update running task status"""
        if self.current_task and self.current_task.label == label:
            self.current_task.warnings = warnings
            self.current_task.errors = errors
            if progress is not None:
                self.current_task.progress = min(99, progress)
            else:
                # Auto-calculate progress from elapsed time
                elapsed = time.time() - self.current_task.start_time
                if self.current_task.estimated_time > 0:
                    self.current_task.progress = min(99, int(100 * elapsed / self.current_task.estimated_time))
            self.current_task.elapsed = time.time() - self.current_task.start_time
    
    @abstractmethod
    def finish(self, label: str, success: bool, warnings: int, errors: int):
        """Mark task as completed"""
        for task in self.tasks:
            if task.label == label:
                task.status = 'done' if success else 'failed'
                task.warnings = warnings
                task.errors = errors
                if task.start_time:
                    task.elapsed = time.time() - task.start_time
                    self._update_ema(label, task.elapsed)
                self.total_warnings += warnings
                self.total_errors += errors
                if self.current_task == task:
                    self.current_task = None
                break
    
    @abstractmethod
    def summary(self):
        """Show final summary"""
        self._save_times()
    
    def close(self):
        """Cleanup on exit"""
        self._save_times()