#!/usr/bin/env python3
"""
Clean FX Banner with off-screen framebuffer, plasma effect, and logo overlay.
No wrapping, proper clipping, truecolor support.
"""

import os
import sys
import time
import math
import random
from typing import List, Tuple, Optional
from dataclasses import dataclass

# Color palette from original spec
PALETTE_HEX = [
    0x000000, 0x1A0033, 0x330066, 0x4D0099, 0x6600CC, 0x7F00FF,
    0x9933FF, 0xB366FF, 0xCC99FF, 0xE6CCFF, 0xFFE6FF, 0xFFCCFF,
    0xFF99FF, 0xFF66FF, 0xFF33FF, 0xFF00FF, 0xFF00CC, 0xFF0099,
    0xFF0066, 0xFF0033, 0xFF0000, 0xFF3300, 0xFF6600, 0xFF9900,
    0xFFCC00, 0xFFFF00, 0xCCFF00, 0x99FF00, 0x66FF00, 0x33FF00,
    0x00FF00, 0x00FF33
]

# Convert to RGB tuples
PALETTE_RGB = [(c >> 16, (c >> 8) & 0xFF, c & 0xFF) for c in PALETTE_HEX]

# Gaussian kernel for 4x4 glow spread (normalized to 96)
GLOW_KERNEL = [
    [1,  3,  4,  3],
    [3,  7, 10,  7],
    [4, 10, 12, 10],
    [3,  7, 10,  7]
]

@dataclass
class GlowPoint:
    x: float
    y: float
    vx: float
    vy: float
    amplitude: float
    omega: float
    phase: float
    color_idx: int

class FxBanner:
    """Off-screen framebuffer FX banner with plasma and logo."""
    
    def __init__(self, logo_lines: List[str], palette: Optional[List[Tuple[int, int, int]]] = None):
        self.width = 80
        self.height = 8
        self.framebuffer = [(0, 0, 0)] * (self.width * self.height)
        self.palette = palette or PALETTE_RGB
        self.logo_lines = logo_lines
        self.t0 = time.monotonic()
        self.frame_count = 0
        
        # Check truecolor support
        self.truecolor = self._supports_truecolor()
        
        # Initialize glow points
        self.glow_points = []
        for _ in range(20):
            self.glow_points.append(GlowPoint(
                x=random.random() * self.width,
                y=random.random() * self.height,
                vx=(random.random() - 0.5) * 0.5,
                vy=(random.random() - 0.5) * 0.5,
                amplitude=random.uniform(0.6, 1.0),
                omega=random.uniform(0.8, 1.2),
                phase=random.random() * math.pi * 2,
                color_idx=random.randint(20, 27)  # Warm colors for glow
            ))
    
    def _supports_truecolor(self) -> bool:
        """Check if terminal supports 24-bit truecolor."""
        colorterm = os.environ.get('COLORTERM', '')
        return colorterm in ['truecolor', '24bit']
    
    def resize(self, width: int, height: int):
        """Resize the framebuffer."""
        self.width = width
        self.height = height
        self.framebuffer = [(0, 0, 0)] * (width * height)
    
    def _plasma(self, x: float, y: float, t: float) -> Tuple[int, int, int]:
        """Calculate plasma color at position."""
        # Normalize coordinates
        nx = x / self.width
        ny = y / self.height
        
        # Classic plasma formula: sum of sines
        v = math.sin(6.0 * nx + t)
        v += math.sin(6.0 * ny + t * 1.3)
        v += math.sin(4.0 * math.hypot(nx - 0.5, ny - 0.5) + t * 0.7)
        
        # Normalize to [0, 1]
        v = (v + 3.0) / 6.0
        
        # Sample from palette
        idx = int(v * (len(self.palette) - 1))
        idx = max(0, min(len(self.palette) - 1, idx))
        return self.palette[idx]
    
    def _render_plasma(self, t: float):
        """Render plasma effect to framebuffer."""
        for y in range(self.height):
            for x in range(self.width):
                idx = y * self.width + x
                self.framebuffer[idx] = self._plasma(x, y, t)
    
    def _add_glow_point(self, point: GlowPoint, t: float):
        """Add a single glow point with gaussian spread."""
        # Calculate intensity with breathing effect
        intensity = point.amplitude * (0.5 + 0.5 * math.sin(point.omega * t + point.phase))
        
        # Get glow color
        glow_color = self.palette[point.color_idx % len(self.palette)]
        
        # Apply 4x4 gaussian kernel
        px, py = int(point.x), int(point.y)
        for dy in range(-1, 3):
            for dx in range(-1, 3):
                x = px + dx
                y = py + dy
                if 0 <= x < self.width and 0 <= y < self.height:
                    # Get kernel weight
                    kx = min(3, max(0, dx + 1))
                    ky = min(3, max(0, dy + 1))
                    weight = GLOW_KERNEL[ky][kx] / 96.0
                    
                    # Calculate glow contribution
                    glow_intensity = intensity * weight
                    
                    # Blend with existing color
                    idx = y * self.width + x
                    r0, g0, b0 = self.framebuffer[idx]
                    r1, g1, b1 = glow_color
                    
                    # Additive blending
                    r = min(255, int(r0 + r1 * glow_intensity))
                    g = min(255, int(g0 + g1 * glow_intensity))
                    b = min(255, int(b0 + b1 * glow_intensity))
                    
                    self.framebuffer[idx] = (r, g, b)
    
    def _update_glow_points(self, t: float):
        """Update and render all glow points."""
        for point in self.glow_points:
            # Update position
            point.x += point.vx
            point.y += point.vy
            
            # Bounce off walls
            if point.x < 0 or point.x >= self.width:
                point.vx = -point.vx
                point.x = max(0, min(self.width - 1, point.x))
            if point.y < 0 or point.y >= self.height:
                point.vy = -point.vy
                point.y = max(0, min(self.height - 1, point.y))
            
            # Render glow
            self._add_glow_point(point, t)
    
    def _overlay_logo(self):
        """Overlay ASCII logo centered on framebuffer."""
        if not self.logo_lines:
            return
        
        logo_height = len(self.logo_lines)
        logo_width = max(len(line) for line in self.logo_lines) if self.logo_lines else 0
        
        # Center position
        start_y = max(0, (self.height - logo_height) // 2)
        start_x = max(0, (self.width - logo_width) // 2)
        
        # Logo colors (cool tones)
        logo_fg = (0, 255, 255)  # Cyan
        logo_bg = (0, 64, 128)   # Dark blue
        
        for ly, line in enumerate(self.logo_lines):
            y = start_y + ly
            if y >= self.height:
                break
            
            for lx, char in enumerate(line):
                x = start_x + lx
                if x >= self.width:
                    break
                
                if char not in (' ', ''):
                    idx = y * self.width + x
                    # Use bright color for logo characters
                    if char in ('█', '╗', '╔', '╝', '╚', '║', '═'):
                        self.framebuffer[idx] = logo_fg
                    else:
                        self.framebuffer[idx] = logo_bg
    
    def frame(self, now: Optional[float] = None) -> 'RenderOutput':
        """Render a frame and return as Rich Text or ANSI string."""
        if now is None:
            now = time.monotonic()
        
        t = now - self.t0
        self.frame_count += 1
        
        # Render layers
        self._render_plasma(t)
        self._update_glow_points(t)
        self._overlay_logo()
        
        # Convert to output format
        try:
            from rich.text import Text
            return self._to_rich_text()
        except ImportError:
            return self._to_ansi_string()
    
    def _to_rich_text(self):
        """Convert framebuffer to Rich Text with no wrapping."""
        from rich.text import Text
        
        output = Text(no_wrap=True, overflow='crop')
        
        for y in range(self.height):
            if y > 0:
                output.append('\n')
            
            for x in range(self.width):
                idx = y * self.width + x
                r, g, b = self.framebuffer[idx]
                
                # Use space with background color for solid fill
                output.append(' ', style=f'on rgb({r},{g},{b})')
        
        return output
    
    def _to_ansi_string(self) -> str:
        """Convert framebuffer to ANSI escape codes."""
        lines = []
        
        for y in range(self.height):
            line = []
            for x in range(self.width):
                idx = y * self.width + x
                r, g, b = self.framebuffer[idx]
                
                if self.truecolor:
                    # 24-bit truecolor background
                    line.append(f'\033[48;2;{r};{g};{b}m ')
                else:
                    # Fallback to 256-color
                    color_idx = self._rgb_to_256(r, g, b)
                    line.append(f'\033[48;5;{color_idx}m ')
            
            # Reset at end of line
            line.append('\033[0m')
            lines.append(''.join(line))
        
        return '\n'.join(lines)
    
    def _rgb_to_256(self, r: int, g: int, b: int) -> int:
        """Convert RGB to xterm-256 color index."""
        # Check for grayscale
        if r == g == b:
            if r < 8:
                return 16  # Black
            if r > 248:
                return 231  # White
            # Gray ramp 232-255
            return 232 + ((r - 8) * 24 // 247)
        
        # Map to 6x6x6 color cube (16-231)
        r6 = r * 5 // 255
        g6 = g * 5 // 255
        b6 = b * 5 // 255
        
        return 16 + 36 * r6 + 6 * g6 + b6

def demo():
    """Standalone demo of FX banner."""
    logo = [
        " ███████╗ █████╗ ███████╗████████╗███████╗███████╗███╗   ███╗",
        " ██╔════╝██╔══██╗██╔════╝╚══██╔══╝██╔════╝██╔════╝████╗ ████║",
        " █████╗  ███████║███████╗   ██║   █████╗  ███████╗██╔████╔██║",
        " ██╔══╝  ██╔══██║╚════██║   ██║   ██╔══╝  ╚════██║██║╚██╔╝██║",
        " ██║     ██║  ██║███████║   ██║   ██║     ███████║██║ ╚═╝ ██║",
        " ╚═╝     ╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝     ╚══════╝╚═╝     ╚═╝"
    ]
    
    # Get terminal size
    try:
        import shutil
        width, height = shutil.get_terminal_size((80, 24))
    except:
        width, height = 80, 24
    
    # Create banner with appropriate height
    fx_height = min(len(logo) + 2, 10)
    fx = FxBanner(logo)
    fx.resize(width, fx_height)
    
    # Clear screen
    print('\033[2J\033[H', end='')
    
    # Animation loop
    try:
        fps = 15
        frame_time = 1.0 / fps
        duration = 10.0
        start = time.monotonic()
        
        while time.monotonic() - start < duration:
            frame_start = time.monotonic()
            
            # Render frame
            output = fx.frame()
            
            # Position cursor and display
            print('\033[H', end='')
            if isinstance(output, str):
                print(output)
            else:
                # Rich Text
                from rich.console import Console
                console = Console()
                console.print(output)
            
            # Maintain FPS
            elapsed = time.monotonic() - frame_start
            if elapsed < frame_time:
                time.sleep(frame_time - elapsed)
    
    except KeyboardInterrupt:
        pass
    finally:
        # Clear and reset
        print('\033[0m\033[2J\033[H')

if __name__ == '__main__':
    demo()