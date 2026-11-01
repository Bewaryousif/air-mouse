"""
Mouse controller.

Translates gesture results into real OS-level mouse actions using PyAutoGUI.
Cursor motion is smoothed and mapped from the camera's "active region" to the
full screen so a small, comfortable hand movement covers the whole display.
"""

from __future__ import annotations

from typing import Optional, Tuple

import pyautogui

from config import Config
from gesture_recognizer import Gesture, GestureResult


class MouseController:
    def __init__(self, config: Config):
        self._cfg = config
        self.screen_w, self.screen_h = pyautogui.size()

        # Disable PyAutoGUI's per-call pause for responsiveness.
        pyautogui.PAUSE = 0
        pyautogui.FAILSAFE = config.failsafe

        # Smoothing state (previous cursor position).
        self._prev_x: Optional[float] = None
        self._prev_y: Optional[float] = None

        self._is_mouse_down = False

    # ------------------------------------------------------------------ #
    def apply(self, result: GestureResult) -> None:
        """Perform the OS action for a recognised gesture."""
        g = result.gesture

        if g in (Gesture.MOVE, Gesture.DRAG_MOVE, Gesture.DRAG_START):
            if result.cursor_point is not None:
                self._move_to(result.cursor_point)

        if g == Gesture.DRAG_START:
            if not self._is_mouse_down:
                pyautogui.mouseDown(button="left")
                self._is_mouse_down = True

        elif g == Gesture.DRAG_END:
            if self._is_mouse_down:
                pyautogui.mouseUp(button="left")
                self._is_mouse_down = False

        elif g == Gesture.LEFT_CLICK:
            pyautogui.click(button="left")

        elif g == Gesture.DOUBLE_CLICK:
            pyautogui.doubleClick()

        elif g == Gesture.RIGHT_CLICK:
            pyautogui.click(button="right")

        elif g == Gesture.SCROLL:
            if result.scroll_amount:
                pyautogui.scroll(result.scroll_amount)

    # ------------------------------------------------------------------ #
    def _move_to(self, norm_point: Tuple[float, float]) -> None:
        """Map a normalised camera point to a smoothed screen position."""
        nx, ny = norm_point

        # Map the active region (inset by frame_margin) to the full screen.
        margin_x = self._cfg.frame_margin / self._cfg.frame_width
        margin_y = self._cfg.frame_margin / self._cfg.frame_height

        usable_x = max(1e-6, 1.0 - 2 * margin_x)
        usable_y = max(1e-6, 1.0 - 2 * margin_y)

        rel_x = (nx - margin_x) / usable_x
        rel_y = (ny - margin_y) / usable_y

        rel_x = min(max(rel_x, 0.0), 1.0)
        rel_y = min(max(rel_y, 0.0), 1.0)

        target_x = rel_x * self.screen_w
        target_y = rel_y * self.screen_h

        # Exponential smoothing toward the target.
        if self._prev_x is None:
            self._prev_x, self._prev_y = target_x, target_y

        s = max(self._cfg.smoothing, 1.0)
        new_x = self._prev_x + (target_x - self._prev_x) / s
        new_y = self._prev_y + (target_y - self._prev_y) / s

        if (abs(new_x - self._prev_x) < self._cfg.move_deadzone and
                abs(new_y - self._prev_y) < self._cfg.move_deadzone):
            return

        self._prev_x, self._prev_y = new_x, new_y

        # Clamp inside the screen with a 1px border to avoid the fail-safe.
        x = min(max(int(new_x), 1), self.screen_w - 2)
        y = min(max(int(new_y), 1), self.screen_h - 2)
        pyautogui.moveTo(x, y)

    # ------------------------------------------------------------------ #
    def release(self) -> None:
        """Make sure no button is left pressed when shutting down."""
        if self._is_mouse_down:
            try:
                pyautogui.mouseUp(button="left")
            except Exception:
                pass
            self._is_mouse_down = False
