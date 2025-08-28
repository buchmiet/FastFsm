#!/usr/bin/env python3
"""Show TUI features comparison"""

import time
import sys

# Test if Rich is available
try:
    from rich.console import Console
    from rich.table import Table
    from rich.panel import Panel
    from rich.layout import Layout
    from rich import box
    console = Console()
    RICH_AVAILABLE = True
except ImportError:
    RICH_AVAILABLE = False
    print("Rich not available - install with: pip install rich")

def show_rich_capabilities():
    """Show what Rich TUI can do"""
    
    console.print("\n[bold cyan]Rich TUI Capabilities:[/bold cyan]\n")
    
    # Create a sample status table
    table = Table(title="Build Status", box=box.ROUNDED, show_header=True)
    table.add_column("", width=2)
    table.add_column("Task", style="white", width=30)
    table.add_column("Time", justify="right", width=8)
    table.add_column("W/E", justify="right", width=8)
    table.add_column("Progress", justify="right", width=10)
    
    # Add sample rows with different states
    table.add_row("·", "[dim]Initialize project[/dim]", "", "", "")
    table.add_row("[cyan]▶[/cyan]", "[bold cyan]Download dependencies[/bold cyan]", "2.3s", "[yellow]2[/yellow]/0", "[cyan]45%[/cyan]")
    table.add_row("[green]✓[/green]", "Compile sources", "5.1s", "", "100%")
    table.add_row("[red]✗[/red]", "[red]Run unit tests[/red]", "3.2s", "0/[red]3[/red]", "")
    table.add_row("·", "[dim]Package artifacts[/dim]", "", "", "")
    table.add_row("·", "[dim]Deploy to server[/dim]", "", "", "")
    
    console.print(table)
    
    console.print("\n[bold green]Features:[/bold green]")
    console.print("• Live updating table with colors")
    console.print("• Progress percentages and spinners")
    console.print("• Warning/Error counters with highlighting")
    console.print("• Elapsed time tracking")
    console.print("• Clean, professional appearance")
    
    # Show comparison panel
    panel = Panel.fit(
        "[cyan]ANSI Mode:[/cyan] Basic colors, manual positioning\n"
        "[green]Rich Mode:[/green] Advanced tables, auto-layout, smooth updates\n"
        "[dim]Plain Mode:[/dim] No colors, CI/pipe friendly",
        title="TUI Mode Comparison",
        border_style="blue"
    )
    console.print("\n", panel)

def show_ansi_sample():
    """Show ANSI TUI sample"""
    print("\n\033[1;36mANSI TUI Sample:\033[0m\n")
    print("=" * 60)
    print(" \033[90m·\033[0m Initialize project")
    print(" \033[36m▶\033[0m \033[1;36mDownload dependencies\033[0m    2.3s  \033[33mW:2\033[0m    [45%]")
    print(" \033[32m✓\033[0m Compile sources           5.1s         [100%]")
    print(" \033[31m✗\033[0m Run unit tests            3.2s  \033[31mE:3\033[0m")
    print(" \033[90m·\033[0m Package artifacts")
    print(" \033[90m·\033[0m Deploy to server")
    print("\033[90mTotal: 10.6s | W:2 E:3\033[0m")
    print("=" * 60)

if __name__ == "__main__":
    print("=" * 70)
    print("TUI FEATURES DEMONSTRATION")
    print("=" * 70)
    
    if RICH_AVAILABLE:
        show_rich_capabilities()
    else:
        print("\nRich not installed - showing ANSI fallback:")
    
    show_ansi_sample()
    
    print("\n" + "=" * 70)
    print("AVAILABLE MODES:")
    print("  --ui auto   : Auto-detect (Rich → ANSI → Plain)")
    print("  --ui rich   : Force Rich (requires 'pip install rich')")
    print("  --ui ansi   : ANSI colors with cursor control")
    print("  --ui plain  : No colors, CI-friendly")
    print("  --progress  : Show % progress (with time estimates)")
    print("  --no-color  : Disable all colors")
    print("=" * 70)