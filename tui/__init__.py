"""TUI abstraction layer for build.py"""
from .base import ITui
from .plain import PlainTui
from .ansi import AnsiTui

def create_tui(mode='auto', use_progress=False, use_color=True) -> ITui:
    """Factory for TUI implementations"""
    import sys
    
    if mode == 'plain' or not sys.stdout.isatty():
        return PlainTui()
    
    # Try Rich first
    if mode in ('auto', 'rich'):
        try:
            from .richui import RichTui
            return RichTui(use_progress=use_progress, use_color=use_color)
        except ImportError:
            if mode == 'rich':
                print("Warning: Rich not installed, falling back to ANSI")
    
    # Fallback to ANSI
    if use_color and sys.stdout.isatty():
        return AnsiTui(use_progress=use_progress)
    else:
        return PlainTui()

def show_loader(duration: float = 3.0, width: int = 80, height: int = 24):
    """Show demoscene-style loader animation."""
    import sys
    try:
        from .logofx import LogoFX
        if sys.stdout.isatty():
            fx = LogoFX(width=width, height=height)
            fx.run(duration=duration)
    except Exception:
        # Silently fail if loader can't run
        pass

__all__ = ['ITui', 'create_tui', 'PlainTui', 'AnsiTui', 'show_loader']