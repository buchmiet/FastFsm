#!/usr/bin/env python3
"""
Enhanced Rich TUI with parallel FX rendering and new layout.
"""

import sys
import time
import threading
import math
import random
from typing import List, Dict, Optional, Any
from dataclasses import dataclass, field
from datetime import datetime

try:
    from rich.console import Console
    from rich.live import Live
    from rich.layout import Layout
    from rich.panel import Panel
    from rich.progress import Progress, TextColumn, BarColumn, TaskProgressColumn, TimeElapsedColumn
    from rich.table import Table
    from rich.text import Text
    from rich.align import Align
    from rich import box
    RICH_AVAILABLE = True
except ImportError:
    RICH_AVAILABLE = False

from .base import ITui, TaskInfo

# Import clean FX banner
try:
    from .fx_banner import FxBanner, PALETTE_RGB
    FX_AVAILABLE = True
except ImportError:
    FX_AVAILABLE = False
    FxBanner = None

# ASCII Logo for FastFSM
LOGO_LINES = [
    " ███████╗ █████╗ ███████╗████████╗███████╗███████╗███╗   ███╗",
    " ██╔════╝██╔══██╗██╔════╝╚══██╔══╝██╔════╝██╔════╝████╗ ████║",
    " █████╗  ███████║███████╗   ██║   █████╗  ███████╗██╔████╔██║",
    " ██╔══╝  ██╔══██║╚════██║   ██║   ██╔══╝  ╚════██║██║╚██╔╝██║",
    " ██║     ██║  ██║███████║   ██║   ██║     ███████║██║ ╚═╝ ██║",
    " ╚═╝     ╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝     ╚══════╝╚═╝     ╚═╝"
]

# Large digit font (5 lines tall)
DIGITS = {
    '0': ["███", "█ █", "█ █", "█ █", "███"],
    '1': [" █ ", "██ ", " █ ", " █ ", "███"],
    '2': ["███", "  █", "███", "█  ", "███"],
    '3': ["███", "  █", "███", "  █", "███"],
    '4': ["█ █", "█ █", "███", "  █", "  █"],
    '5': ["███", "█  ", "███", "  █", "███"],
    '6': ["███", "█  ", "███", "█ █", "███"],
    '7': ["███", "  █", " █ ", " █ ", " █ "],
    '8': ["███", "█ █", "███", "█ █", "███"],
    '9': ["███", "█ █", "███", "  █", "███"],
    ':': ["   ", " █ ", "   ", " █ ", "   "]
}


class RichTuiV2(ITui):
    """Enhanced Rich TUI with parallel rendering."""
    
    def __init__(self, use_progress=True, use_color=True):
        super().__init__()
        self.console = Console()
        self.tasks_data = {}
        self.lock = threading.Lock()
        self.start_time = time.time()
        self.running = True
        
        # Layout components
        self.layout = None
        self.live = None
        self.fx = None
        
        # Metadata
        self.branch = ""
        self.version = ""
        self.total_warnings = 0
        self.total_errors = 0
    
    def set_metadata(self, branch: str, version: str):
        """Set branch and version info."""
        self.branch = branch
        self.version = version
    
    def register(self, tasks: List[TaskInfo]):
        """Register all tasks."""
        with self.lock:
            for task in tasks:
                # Use task.id if available, otherwise generate from label
                task_id = getattr(task, 'id', task.label.replace(' ', '_'))
                self.tasks_data[task_id] = {
                    'info': task,
                    'status': 'pending',
                    'warnings': 0,
                    'errors': 0,
                    'start_time': None,
                    'end_time': None,
                    'progress': 0
                }
    
    def start(self, task_id: str):
        """Start a task."""
        with self.lock:
            if task_id in self.tasks_data:
                self.tasks_data[task_id]['status'] = 'running'
                self.tasks_data[task_id]['start_time'] = time.time()
    
    def update(self, task_id: str, warning=False, error=False, line=None):
        """Update task with warnings/errors."""
        with self.lock:
            if task_id in self.tasks_data:
                if warning:
                    self.tasks_data[task_id]['warnings'] += 1
                    self.total_warnings += 1
                if error:
                    self.tasks_data[task_id]['errors'] += 1
                    self.total_errors += 1
                
                # Update progress based on output (simplified)
                task = self.tasks_data[task_id]
                if task['status'] == 'running':
                    # Simulate progress based on time
                    elapsed = time.time() - task['start_time']
                    estimated = task['info'].estimated_time or 5.0
                    task['progress'] = min(99, int(elapsed / estimated * 100))
    
    def complete(self, task_id: str, failed=False):
        """Complete a task (alias for compatibility)."""
        with self.lock:
            if task_id in self.tasks_data:
                self.tasks_data[task_id]['status'] = 'failed' if failed else 'completed'
                self.tasks_data[task_id]['end_time'] = time.time()
                # Always set to 100% when completed
                self.tasks_data[task_id]['progress'] = 100
    
    def _create_layout(self):
        """Create the layout structure."""
        # Calculate FX height based on logo
        logo_height = len(LOGO_LINES)
        fx_height = min(logo_height + 1, max(8, logo_height + 4))  # Clamp properly
        
        # Get terminal size
        width = self.console.width
        height = self.console.height
        
        # Initialize clean FX banner
        if FX_AVAILABLE:
            self.fx = FxBanner(LOGO_LINES)
            self.fx.resize(width, fx_height)
        else:
            self.fx = None
        
        # Create layout
        self.layout = Layout()
        
        # Split into top (FX) and bottom (main content)
        self.layout.split_column(
            Layout(name="fx", size=fx_height),
            Layout(name="main")
        )
        
        # Split main into table (left) and sidebar (right)
        self.layout["main"].split_row(
            Layout(name="table", ratio=3),
            Layout(name="sidebar", ratio=1)
        )
    
    def _render_fx(self):
        """Render FX banner without Panel wrapper."""
        if self.fx:
            # Get current time for animation
            now = time.time()
            # Return the framebuffer output directly (no Panel!)
            return self.fx.frame(now)
        else:
            # Fallback to simple text
            from rich.text import Text
            return Text('\n'.join(LOGO_LINES), style="cyan", no_wrap=True, overflow='crop')
    
    def _render_table(self) -> Panel:
        """Render tasks table with progress."""
        # Create table with simple header line
        from rich.table import Table
        from rich import box
        
        table = Table(
            show_header=True,
            header_style="bold cyan",
            box=box.SIMPLE_HEAD,  # Only horizontal line under header
            expand=True,
            pad_edge=False,
            show_lines=False,  # No lines between rows
            padding=(0, 1)  # Horizontal padding for columns
        )
        
        table.add_column("Task", style="white", width=40)
        table.add_column("Time", style="yellow", width=8)
        table.add_column("W/E", style="red", width=8)
        table.add_column("Progress", width=20)
        
        with self.lock:
            for task_id, data in self.tasks_data.items():
                info = data['info']
                status = data['status']
                
                # Status icon
                if status == 'completed':
                    icon = "✓"
                    style = "green"
                elif status == 'failed':
                    icon = "✗"
                    style = "red"
                elif status == 'running':
                    icon = "▶"
                    style = "cyan"
                else:
                    icon = "·"
                    style = "dim"
                
                # Task name (use name if available, otherwise label)
                task_name = getattr(info, 'name', info.label)
                task_text = Text(f"{icon} {task_name}", style=style)
                
                # Time
                if data['start_time']:
                    if data['end_time']:
                        elapsed = data['end_time'] - data['start_time']
                    else:
                        elapsed = time.time() - data['start_time']
                    time_text = f"{elapsed:.1f}s"
                else:
                    time_text = "–"
                
                # Warnings/Errors
                w_e = f"{data['warnings']}/{data['errors']}"
                
                # Progress bar with centered percentage
                progress = data['progress']
                bar_width = 20
                filled = int(bar_width * progress / 100)
                empty = bar_width - filled
                
                # Build simple green bar with white percentage overlay
                percentage_str = f"{progress:3d}%"
                bar_str = "█" * filled + "░" * empty
                
                # Center the percentage text
                pad_left = (bar_width - len(percentage_str)) // 2
                pad_right = bar_width - pad_left - len(percentage_str)
                
                # Create the progress bar text with styling
                progress_bar = Text()
                for i, char in enumerate(bar_str):
                    if i >= pad_left and i < pad_left + len(percentage_str):
                        # Overlay percentage text
                        percent_char = percentage_str[i - pad_left]
                        if char == "█":
                            progress_bar.append(percent_char, style="bold white on green")
                        else:
                            progress_bar.append(percent_char, style="bold white on dim")
                    else:
                        # Regular bar character
                        if char == "█":
                            progress_bar.append(char, style="green")
                        else:
                            progress_bar.append(char, style="dim")
                
                table.add_row(task_text, time_text, w_e, progress_bar)
                
                # Add empty row for spacing (except after last item)
                if task_id != list(self.tasks_data.keys())[-1]:
                    table.add_row("", "", "", "")
        
        # Add global progress bar
        completed = sum(1 for d in self.tasks_data.values() if d['status'] in ('completed', 'failed'))
        total = len(self.tasks_data)
        global_progress = int((completed / total * 100)) if total > 0 else 0
        
        # Create a separate panel for global progress
        progress_bar_width = 50
        filled = int(progress_bar_width * global_progress / 100)
        empty = progress_bar_width - filled
        
        # Build global progress bar with centered percentage
        percentage_str = f"{global_progress:3d}%"
        bar_str = "█" * filled + "░" * empty
        
        # Center the percentage text
        pad_left = (progress_bar_width - len(percentage_str)) // 2
        
        # Create styled global progress bar
        global_bar = Text()
        for i, char in enumerate(bar_str):
            if i >= pad_left and i < pad_left + len(percentage_str):
                # Overlay percentage text
                percent_char = percentage_str[i - pad_left]
                if char == "█":
                    global_bar.append(percent_char, style="bold white on green")
                else:
                    global_bar.append(percent_char, style="bold white on dim")
            else:
                # Regular bar character
                if char == "█":
                    global_bar.append(char, style="green")
                else:
                    global_bar.append(char, style="dim")
        
        elapsed = time.time() - self.start_time
        if global_progress > 0 and global_progress < 100:
            eta = elapsed * (100 - global_progress) / global_progress
            eta_text = f"  ETA {int(eta//60):02d}:{int(eta%60):02d}"
        else:
            eta_text = "  ETA --:--"
        
        # Add ETA text to the bar
        global_bar.append(eta_text, style="cyan")
        
        progress_panel = Panel(
            global_bar,
            box=box.SIMPLE,
            style="cyan"
        )
        
        # Combine table and progress
        from rich.console import Group
        return Panel(
            Group(table, progress_panel),
            title=f"[bold]Branch: {self.branch} | Version: {self.version}[/bold]",
            box=box.ROUNDED
        )
    
    def _render_sidebar(self) -> Panel:
        """Render sidebar with clock and info."""
        elapsed = int(time.time() - self.start_time)
        minutes = elapsed // 60
        seconds = elapsed % 60
        time_str = f"{minutes:02d}:{seconds:02d}"
        
        # Build large clock display
        clock_lines = ['', '', '', '', '']
        for char in time_str:
            if char in DIGITS:
                for i, line in enumerate(DIGITS[char]):
                    clock_lines[i] += line + " "
        
        clock_text = '\n'.join(clock_lines)
        
        # Additional info
        info_text = f"\n\n[bold cyan]Build Info[/bold cyan]\n"
        info_text += f"Branch: {self.branch}\n"
        info_text += f"Version: {self.version}\n"
        info_text += f"Warnings: [yellow]{self.total_warnings}[/yellow]\n"
        info_text += f"Errors: [red]{self.total_errors}[/red]\n"
        
        return Panel(
            Align.center(clock_text + info_text),
            title="[bold]⏱ Timer[/bold]",
            box=box.ROUNDED,
            style="cyan"
        )
    
    def _update_layout(self):
        """Update the layout with current data."""
        if self.layout:
            self.layout["fx"].update(self._render_fx())
            self.layout["table"].update(self._render_table())
            self.layout["sidebar"].update(self._render_sidebar())
    
    def run(self):
        """Run the TUI with live updates."""
        if not RICH_AVAILABLE:
            return
        
        self._create_layout()
        
        # Start live display
        with Live(
            self.layout,
            console=self.console,
            refresh_per_second=10,
            screen=True
        ) as live:
            self.live = live
            
            # Update loop
            while self.running:
                self._update_layout()
                time.sleep(0.1)  # 10 FPS
                
                # Check if all tasks are done
                with self.lock:
                    all_done = all(
                        d['status'] in ('completed', 'failed')
                        for d in self.tasks_data.values()
                    )
                    if all_done and len(self.tasks_data) > 0:
                        break
    
    def stop(self):
        """Stop the TUI."""
        self.running = False
    
    def finish(self, task_id_or_label: str, success: bool, warnings: int, errors: int):
        """Mark task as completed (required by base class)."""
        # Handle both task_id and label
        task_id = None
        with self.lock:
            # First check if it's directly a task_id
            if task_id_or_label in self.tasks_data:
                task_id = task_id_or_label
            else:
                # Otherwise try to match by label
                for tid, data in self.tasks_data.items():
                    if data['info'].label == task_id_or_label:
                        task_id = tid
                        break
        
        if task_id:
            with self.lock:
                self.tasks_data[task_id]['status'] = 'completed' if success else 'failed'
                self.tasks_data[task_id]['end_time'] = time.time()
                self.tasks_data[task_id]['progress'] = 100
                self.tasks_data[task_id]['warnings'] = warnings
                self.tasks_data[task_id]['errors'] = errors
    
    def summary(self):
        """Print final summary."""
        self.stop()
        
        # Final update
        self._update_layout()
        time.sleep(0.5)  # Let final frame render
        
        # Print summary
        self.console.print("\n[bold green]Build Complete![/bold green]")
        self.console.print(f"Total warnings: [yellow]{self.total_warnings}[/yellow]")
        self.console.print(f"Total errors: [red]{self.total_errors}[/red]")