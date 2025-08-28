#!/usr/bin/env python3
"""
Demoscene-style loader with plasma effects and ASCII logo.
Max 25KB code size limit.
"""

import os
import sys
import time
import math
import random
from typing import List, Tuple, Optional
from dataclasses import dataclass
import threading

# Color palette from spec
PALETTE_HEX = [
    0x000000, 0x1A0033, 0x330066, 0x4D0099, 0x6600CC, 0x7F00FF,
    0x9933FF, 0xB366FF, 0xCC99FF, 0xE6CCFF, 0xFFE6FF, 0xFFCCFF,
    0xFF99FF, 0xFF66FF, 0xFF33FF, 0xFF00FF, 0xFF00CC, 0xFF0099,
    0xFF0066, 0xFF0033, 0xFF0000, 0xFF3300, 0xFF6600, 0xFF9900,
    0xFFCC00, 0xFFFF00, 0xCCFF00, 0x99FF00, 0x66FF00, 0x33FF00,
    0x00FF00, 0x00FF33
]

# Convert to RGB tuples
PALETTE = [(c >> 16, (c >> 8) & 0xFF, c & 0xFF) for c in PALETTE_HEX]

# ASCII Logo (compact version)
LOGO = """
 ███████╗ █████╗ ███████╗████████╗███████╗███████╗███╗   ███╗
 ██╔════╝██╔══██╗██╔════╝╚══██╔══╝██╔════╝██╔════╝████╗ ████║
 █████╗  ███████║███████╗   ██║   █████╗  ███████╗██╔████╔██║
 ██╔══╝  ██╔══██║╚════██║   ██║   ██╔══╝  ╚════██║██║╚██╔╝██║
 ██║     ██║  ██║███████║   ██║   ██║     ███████║██║ ╚═╝ ██║
 ╚═╝     ╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝     ╚══════╝╚═╝     ╚═╝
""".strip().split('\n')

@dataclass
class GlowPoint:
    x: float
    y: float
    vx: float
    vy: float
    intensity: float
    color_idx: int

class LogoFX:
    def __init__(self, width: int = 80, height: int = 24):
        self.width = width
        self.height = height
        self.frame = 0
        self.start_time = time.time()
        self.running = True
        
        # Plasma parameters
        self.plasma_scale = 0.15
        self.plasma_speed = 0.02
        
        # Glowing points
        self.glow_points: List[GlowPoint] = []
        for _ in range(20):
            self.glow_points.append(GlowPoint(
                x=random.random() * width,
                y=random.random() * height,
                vx=(random.random() - 0.5) * 0.5,
                vy=(random.random() - 0.5) * 0.5,
                intensity=random.random() * 0.5 + 0.5,
                color_idx=random.randint(10, 25)
            ))
        
        # Frame buffer
        self.buffer = [[0 for _ in range(width)] for _ in range(height)]
        self.char_buffer = [[' ' for _ in range(width)] for _ in range(height)]
        
        # Terminal capabilities
        self.truecolor = self._check_truecolor()
        
        # Hide cursor and setup terminal
        print('\033[?25l', end='')  # Hide cursor
        print('\033[2J\033[H', end='')  # Clear screen
        
    def _check_truecolor(self) -> bool:
        """Check if terminal supports truecolor."""
        colorterm = os.environ.get('COLORTERM', '')
        return colorterm in ['truecolor', '24bit']
    
    def _plasma(self, x: float, y: float, t: float) -> float:
        """Calculate plasma value at position."""
        v1 = math.sin(x * self.plasma_scale + t)
        v2 = math.sin(self.plasma_scale * (x * math.sin(t / 2) + y * math.cos(t / 3)) + t)
        cx = x + 0.5 * math.sin(t / 5)
        cy = y + 0.5 * math.cos(t / 3)
        v3 = math.sin(math.sqrt(self.plasma_scale * self.plasma_scale * (cx * cx + cy * cy) + 1) + t)
        return (v1 + v2 + v3) / 3
    
    def _gaussian_spread(self, cx: float, cy: float, intensity: float, color_idx: int):
        """Apply gaussian glow spread 4x4."""
        kernel = [
            [0.003, 0.013, 0.022, 0.013, 0.003],
            [0.013, 0.060, 0.098, 0.060, 0.013],
            [0.022, 0.098, 0.162, 0.098, 0.022],
            [0.013, 0.060, 0.098, 0.060, 0.013],
            [0.003, 0.013, 0.022, 0.013, 0.003]
        ]
        
        for dy in range(-2, 3):
            for dx in range(-2, 3):
                x = int(cx + dx)
                y = int(cy + dy)
                if 0 <= x < self.width and 0 <= y < self.height:
                    k = kernel[dy + 2][dx + 2]
                    self.buffer[y][x] = min(31, self.buffer[y][x] + int(k * intensity * 31))
    
    def _render_plasma(self):
        """Render plasma background."""
        t = self.frame * self.plasma_speed
        
        for y in range(self.height):
            for x in range(self.width):
                v = self._plasma(x, y, t)
                # Map to palette index (0-15 for background)
                idx = int((v + 1) * 7.5)
                self.buffer[y][x] = max(0, min(15, idx))
    
    def _update_glow_points(self):
        """Update and render glowing points."""
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
            
            # Apply gaussian glow
            self._gaussian_spread(point.x, point.y, point.intensity, point.color_idx)
    
    def _render_logo(self):
        """Overlay ASCII logo in center."""
        logo_height = len(LOGO)
        logo_width = max(len(line) for line in LOGO)
        
        start_y = (self.height - logo_height) // 2
        start_x = (self.width - logo_width) // 2
        
        for y, line in enumerate(LOGO):
            for x, char in enumerate(line):
                if char != ' ':
                    py = start_y + y
                    px = start_x + x
                    if 0 <= py < self.height and 0 <= px < self.width:
                        self.char_buffer[py][px] = char
                        # Brighten logo area
                        self.buffer[py][px] = min(31, self.buffer[py][px] + 10)
    
    def _render_clock(self):
        """Render large digit clock at bottom."""
        elapsed = int(time.time() - self.start_time)
        minutes = elapsed // 60
        seconds = elapsed % 60
        time_str = f"{minutes:02d}:{seconds:02d}"
        
        # Large digits (3x5)
        digits = {
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
        
        clock_y = self.height - 7
        clock_x = (self.width - len(time_str) * 4) // 2
        
        for i, char in enumerate(time_str):
            if char in digits:
                for y, row in enumerate(digits[char]):
                    for x, c in enumerate(row):
                        if c != ' ':
                            py = clock_y + y
                            px = clock_x + i * 4 + x
                            if 0 <= py < self.height and 0 <= px < self.width:
                                self.char_buffer[py][px] = c
                                self.buffer[py][px] = 25  # Bright yellow
    
    def _color_to_ansi(self, idx: int) -> str:
        """Convert palette index to ANSI color."""
        if idx >= len(PALETTE):
            idx = idx % len(PALETTE)
        
        r, g, b = PALETTE[idx]
        
        if self.truecolor:
            return f"\033[38;2;{r};{g};{b}m"
        else:
            # Map to 256 colors
            if r == g == b:
                # Grayscale
                gray = r // 10
                return f"\033[38;5;{232 + gray}m"
            else:
                # 6x6x6 color cube
                r6 = r * 5 // 255
                g6 = g * 5 // 255
                b6 = b * 5 // 255
                color = 16 + 36 * r6 + 6 * g6 + b6
                return f"\033[38;5;{color}m"
    
    def render_frame(self):
        """Render complete frame."""
        # Clear buffers
        self.buffer = [[0 for _ in range(self.width)] for _ in range(self.height)]
        self.char_buffer = [[' ' for _ in range(self.width)] for _ in range(self.height)]
        
        # Render layers
        self._render_plasma()
        self._update_glow_points()
        self._render_logo()
        self._render_clock()
        
        # Output to terminal
        output = []
        output.append('\033[H')  # Home cursor
        
        for y in range(self.height):
            for x in range(self.width):
                char = self.char_buffer[y][x]
                if char == ' ':
                    # Use block character for plasma
                    intensity = self.buffer[y][x] / 31.0
                    if intensity < 0.125:
                        char = ' '
                    elif intensity < 0.25:
                        char = '░'
                    elif intensity < 0.5:
                        char = '▒'
                    elif intensity < 0.75:
                        char = '▓'
                    else:
                        char = '█'
                
                color_idx = self.buffer[y][x]
                output.append(self._color_to_ansi(color_idx))
                output.append(char)
            
            output.append('\033[0m\n')
        
        sys.stdout.write(''.join(output))
        sys.stdout.flush()
        
        self.frame += 1
    
    def run(self, duration: float = 10.0):
        """Run animation for specified duration."""
        end_time = time.time() + duration
        fps_target = 15  # Target FPS
        frame_time = 1.0 / fps_target
        
        try:
            while self.running and time.time() < end_time:
                frame_start = time.time()
                
                self.render_frame()
                
                # Maintain FPS
                elapsed = time.time() - frame_start
                if elapsed < frame_time:
                    time.sleep(frame_time - elapsed)
        finally:
            # Restore terminal
            print('\033[?25h', end='')  # Show cursor
            print('\033[0m', end='')  # Reset colors
            print('\033[2J\033[H', end='')  # Clear screen

def demo():
    """Run standalone demo."""
    import termios
    import tty
    import shutil
    
    # Save terminal settings
    old_settings = None
    if sys.stdin.isatty():
        try:
            old_settings = termios.tcgetattr(sys.stdin)
            tty.setraw(sys.stdin.fileno())
        except:
            pass
    
    try:
        # Get terminal size
        try:
            width, height = shutil.get_terminal_size((80, 24))
            height = min(40, height - 1)
            width = min(120, width)
        except:
            width, height = 80, 24
        
        fx = LogoFX(width=width, height=height)
        fx.run(duration=30.0)
    finally:
        # Restore terminal
        if old_settings and sys.stdin.isatty():
            try:
                termios.tcsetattr(sys.stdin, termios.TCSADRAIN, old_settings)
            except:
                pass
        print('\033[0m\033[2J\033[H')

if __name__ == "__main__":
    demo()