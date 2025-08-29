#!/usr/bin/env python3
"""
Enhanced build script with parallel TUI rendering.
"""

import argparse
import os
import subprocess
import sys
import threading
import time
from pathlib import Path
from typing import List, Optional, Tuple
import shutil

# Add TUI module to path
sys.path.insert(0, str(Path(__file__).parent))

from tui import create_tui
from tui.base import TaskInfo

# Try to import enhanced TUI
try:
    from tui.richui_v2 import RichTuiV2, RICH_AVAILABLE
except ImportError:
    RICH_AVAILABLE = False
    RichTuiV2 = None

# Project configuration
ROOT = Path(__file__).parent
NUGET_DIR = ROOT / "nuget"

PACKAGE_IDS = {
    "core": ("FastFsm.Net", ROOT / "FastFsm" / "FastFsm.csproj"),
    "di": ("FastFsm.Net.DependencyInjection", ROOT / "FastFsm.DependencyInjection" / "FastFsm.DependencyInjection.csproj"),
    "log": ("FastFsm.Net.Logging", ROOT / "FastFsm.Logging" / "FastFsm.Logging.csproj"),
}

TEST_PROJECTS = [
    ROOT / "FastFsm.Tests" / "FastFsm.Tests.csproj",
    ROOT / "FastFsm.DependencyInjection.Tests" / "FastFsm.DependencyInjection.Tests.csproj",
    ROOT / "FastFsm.Logging.Tests" / "FastFsm.Logging.Tests.csproj",
]

class BuildRunner:
    """Build runner with parallel TUI support."""
    
    def __init__(self, ui, args):
        self.ui = ui
        self.args = args
        self.lock = threading.Lock()
    
    def run_command(self, cmd: List[str], task_id: str, cwd: Optional[Path] = None) -> Tuple[int, str, str]:
        """Run a command and update UI in real-time."""
        self.ui.start(task_id)
        
        try:
            process = subprocess.Popen(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                cwd=cwd,
                bufsize=1,
                universal_newlines=True
            )
            
            stdout_lines = []
            stderr_lines = []
            
            # Read output line by line
            try:
                import select
                import fcntl
                
                # Make streams non-blocking (Unix only)
                for stream in [process.stdout, process.stderr]:
                    if stream:
                        fd = stream.fileno()
                        fl = fcntl.fcntl(fd, fcntl.F_GETFL)
                        fcntl.fcntl(fd, fcntl.F_SETFL, fl | os.O_NONBLOCK)
                use_select = True
            except ImportError:
                # Windows fallback
                use_select = False
            
            while True:
                # Check if process is still running
                poll = process.poll()
                
                if use_select:
                    # Read available output with select
                    ready = select.select([process.stdout, process.stderr], [], [], 0.1)[0]
                    
                    for stream in ready:
                        if stream == process.stdout:
                            try:
                                line = process.stdout.readline()
                                if line:
                                    stdout_lines.append(line)
                                    # Check for warnings/errors
                                    line_lower = line.lower()
                                    if 'warning' in line_lower:
                                        self.ui.update(task_id, warning=True, line=line)
                                    elif 'error' in line_lower:
                                        self.ui.update(task_id, error=True, line=line)
                                    else:
                                        self.ui.update(task_id, line=line)
                            except:
                                pass
                        
                        elif stream == process.stderr:
                            try:
                                line = process.stderr.readline()
                                if line:
                                    stderr_lines.append(line)
                                    self.ui.update(task_id, error=True, line=line)
                            except:
                                pass
                else:
                    # Windows fallback - simple readline
                    line = process.stdout.readline()
                    if line:
                        stdout_lines.append(line)
                        line_lower = line.lower()
                        if 'warning' in line_lower:
                            self.ui.update(task_id, warning=True, line=line)
                        elif 'error' in line_lower:
                            self.ui.update(task_id, error=True, line=line)
                    
                    line = process.stderr.readline()
                    if line:
                        stderr_lines.append(line)
                        self.ui.update(task_id, error=True, line=line)
                
                # Check if done
                if poll is not None:
                    # Read any remaining output
                    for line in process.stdout:
                        stdout_lines.append(line)
                    for line in process.stderr:
                        stderr_lines.append(line)
                    break
                
                # Small delay to prevent CPU spinning
                if not use_select:
                    time.sleep(0.01)
            
            returncode = process.returncode
            # Use finish method (compatibility with ITui interface)
            if hasattr(self.ui, 'finish'):
                self.ui.finish(task_id, success=(returncode == 0), warnings=0, errors=0)
            elif hasattr(self.ui, 'complete'):
                self.ui.complete(task_id, failed=(returncode != 0))
            
            return returncode, ''.join(stdout_lines), ''.join(stderr_lines)
            
        except Exception as e:
            # Use finish method (compatibility with ITui interface)
            if hasattr(self.ui, 'finish'):
                self.ui.finish(task_id, success=False, warnings=0, errors=1)
            elif hasattr(self.ui, 'complete'):
                self.ui.complete(task_id, failed=True)
            return 1, "", str(e)
    
    def pack_project(self, name: str, csproj: Path, configuration: str) -> bool:
        """Pack a project (also builds it)."""
        task_id = f"pack_{name}"
        cmd = ["dotnet", "pack", str(csproj), "-c", configuration, "-o", str(NUGET_DIR)]
        
        returncode, _, _ = self.run_command(cmd, task_id)
        return returncode == 0
    
    def restore_project(self, csproj: Path) -> bool:
        """Restore a project."""
        task_id = f"restore_{csproj.parent.name}"
        
        # Clear local cache first
        clear_cmd = ["dotnet", "nuget", "locals", "all", "--clear"]
        self.run_command(clear_cmd, f"clear_cache_{csproj.parent.name}")
        
        # Add local source
        cmd = [
            "dotnet", "restore", str(csproj),
            "--source", str(NUGET_DIR),
            "--source", "https://api.nuget.org/v3/index.json",
            "--force", "--no-cache"
        ]
        
        returncode, _, _ = self.run_command(cmd, task_id)
        return returncode == 0
    
    def test_project(self, csproj: Path, configuration: str) -> bool:
        """Test a project."""
        if self.args.no_tests:
            return True
        
        task_id = f"test_{csproj.parent.name}"
        cmd = ["dotnet", "test", str(csproj), "-c", configuration, "--no-build", "--no-restore"]
        
        returncode, _, _ = self.run_command(cmd, task_id)
        return returncode == 0
    
    def build_project(self, csproj: Path, configuration: str) -> bool:
        """Build a project."""
        task_id = f"build_{csproj.parent.name}"
        cmd = ["dotnet", "build", str(csproj), "-c", configuration]
        
        returncode, _, _ = self.run_command(cmd, task_id)
        return returncode == 0

def collect_tasks(args) -> List[TaskInfo]:
    """Collect all tasks to be executed."""
    tasks = []
    
    # Pack tasks (which also build)
    for key, (name, csproj) in PACKAGE_IDS.items():
        task = TaskInfo(
            label=f"pack {name}",
            estimated_time=5.0  # Increased since it also builds
        )
        # Add id as attribute for our tracking
        task.id = f"pack_{name}"
        task.name = f"pack {name}"
        tasks.append(task)
    
    # Restore test projects
    for csproj in TEST_PROJECTS:
        task = TaskInfo(
            label=f"restore {csproj.parent.name}",
            estimated_time=4.0
        )
        task.id = f"restore_{csproj.parent.name}"
        task.name = f"restore {csproj.parent.name}"
        tasks.append(task)
    
    # Test tasks
    if not args.no_tests:
        for csproj in TEST_PROJECTS:
            task = TaskInfo(
                label=f"test {csproj.parent.name}",
                estimated_time=10.0
            )
            task.id = f"test_{csproj.parent.name}"
            task.name = f"test {csproj.parent.name}"
            tasks.append(task)
    
    return tasks

def main():
    """Main entry point."""
    ap = argparse.ArgumentParser(description="Build FastFSM packages")
    ap.add_argument("-c", "--configuration", default="Release",
                    choices=["Debug", "Release"])
    ap.add_argument("--no-tests", action="store_true",
                    help="Skip running tests")
    ap.add_argument("--ui", default="auto",
                    choices=["auto", "rich", "rich2", "ansi", "plain"],
                    help="UI mode (default: auto)")
    ap.add_argument("--plain", action="store_true",
                    help="Force plain output")
    
    args = ap.parse_args()
    
    # UI mode override
    if args.plain:
        args.ui = 'plain'
    
    # Select UI
    if args.ui == 'rich2' and RICH_AVAILABLE and RichTuiV2:
        ui = RichTuiV2(use_progress=True, use_color=True)
        use_enhanced = True
    else:
        ui = create_tui(mode=args.ui if args.ui != 'rich2' else 'auto')
        use_enhanced = False
    
    # Set metadata if enhanced UI
    if use_enhanced and hasattr(ui, 'set_metadata'):
        ui.set_metadata(branch="develop", version="0.6.2.21-develop")
    
    # Collect tasks
    tasks = collect_tasks(args)
    ui.register(tasks)
    
    # Create runner
    runner = BuildRunner(ui, args)
    
    # Start UI in parallel if enhanced
    ui_thread = None
    if use_enhanced and hasattr(ui, 'run'):
        ui_thread = threading.Thread(target=ui.run)
        ui_thread.start()
    
    try:
        # Execute build steps
        NUGET_DIR.mkdir(exist_ok=True)
        
        # Pack projects (which also builds them)
        for key, (name, csproj) in PACKAGE_IDS.items():
            if not runner.pack_project(name, csproj, args.configuration):
                print(f"Pack failed for {name}", file=sys.stderr)
                sys.exit(1)
        
        # Restore test projects
        for csproj in TEST_PROJECTS:
            if not runner.restore_project(csproj):
                print(f"Restore failed for {csproj.parent.name}", file=sys.stderr)
                sys.exit(1)
        
        # Run tests
        if not args.no_tests:
            for csproj in TEST_PROJECTS:
                if not runner.test_project(csproj, args.configuration):
                    print(f"Tests failed for {csproj.parent.name}", file=sys.stderr)
                    # Continue with other tests
        
        # Summary
        ui.summary()
        
        print(f"\nPackages in: {NUGET_DIR}")
        
    finally:
        # Stop UI
        if use_enhanced and hasattr(ui, 'stop'):
            ui.stop()
        if ui_thread:
            ui_thread.join(timeout=2)

if __name__ == "__main__":
    # Make script executable on Linux
    import stat
    script_path = Path(__file__)
    st = os.stat(script_path)
    os.chmod(script_path, st.st_mode | stat.S_IEXEC)
    
    main()