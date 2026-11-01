"""
Gesture recognition.

Converts raw :class:`HandData` into high-level, debounced gestures that the
mouse controller can act on. This is where the "finger language" of the air
mouse is defined:

    Gesture map
    -----------
    * MOVE         : index finger up, others relaxed  -> move the cursor
    * LEFT_CLICK   : pinch thumb + index (quick)       -> single left click
    * DOUBLE_CLICK : two quick thumb+index pinches      -> double click
    * RIGHT_CLICK  : pinch thumb + middle finger        -> right click
    * DRAG         : hold thumb + index pinch           -> press & hold
    * SCROLL       : index + middle up together         -> scroll by hand Y
    * NEUTRAL      : open palm / nothing recognised     -> idle

Every click gesture is debounced with cooldowns and a double-click window so
the recogniser emits clean, intentional events instead of a stream of noise.
"""

from __future__ import annotations

import time
from dataclasses import dataclass
from enum import Enum, auto
from typing import Optional

from config import Config
from hand_tracker import (
    HandData,
    INDEX_TIP,
    MIDDLE_TIP,
    THUMB_TIP,
)


class Gesture(Enum):
    NEUTRAL = auto()
    MOVE = auto()
    LEFT_CLICK = auto()
    DOUBLE_CLICK = auto()
    RIGHT_CLICK = auto()
    DRAG_START = auto()
    DRAG_MOVE = auto()
    DRAG_END = auto()
    SCROLL = auto()


@dataclass
class GestureResult:
    gesture: Gesture
    # Normalised (x, y) in [0,1] of the point used to position the cursor.
    cursor_point: Optional[tuple] = None
    # Positive => scroll up, negative => scroll down (for SCROLL only).
    scroll_amount: int = 0
    # Human readable label for the on-screen overlay.
    label: str = "Neutral"


class GestureRecognizer:
    """Stateful recogniser; call :meth:`update` once per frame."""

    def __init__(self, config: Config):
        self._cfg = config

        # Click / double-click bookkeeping.
        self._last_click_time = 0.0
        self._pending_single_click_time = 0.0
        self._last_right_click_time = 0.0

        # Pinch / drag state.
        self._pinch_active = False
        self._pinch_start_time = 0.0
        self._dragging = False

        # Scroll anchor.
        self._scroll_anchor_y: Optional[float] = None

    # ------------------------------------------------------------------ #
    def update(self, hand: Optional[HandData]) -> GestureResult:
        now = time.time()

        if hand is None:
            # Release a drag if the hand disappears mid-gesture.
            if self._dragging:
                self._dragging = False
                self._pinch_active = False
                return GestureResult(Gesture.DRAG_END, label="Drag end")
            self._reset_transient_state()
            return GestureResult(Gesture.NEUTRAL, label="No hand")

        fingers = hand.fingers_up()
        thumb, index, middle, ring, pinky = fingers

        pinch = hand.normalised_distance(THUMB_TIP, INDEX_TIP)
        middle_pinch = hand.normalised_distance(THUMB_TIP, MIDDLE_TIP)

        index_pt = hand.point(INDEX_TIP)

        # --- 1. Drag: a sustained thumb+index pinch ---------------------
        is_pinching = pinch < self._cfg.pinch_threshold
        if is_pinching:
            if not self._pinch_active:
                self._pinch_active = True
                self._pinch_start_time = now
            held = now - self._pinch_start_time
            if held >= self._cfg.drag_hold_time:
                if not self._dragging:
                    self._dragging = True
                    return GestureResult(
                        Gesture.DRAG_START, cursor_point=index_pt,
                        label="Drag start",
                    )
                return GestureResult(
                    Gesture.DRAG_MOVE, cursor_point=index_pt, label="Dragging",
                )
            # Still pinching but not long enough yet -> wait.
            return GestureResult(
                Gesture.MOVE, cursor_point=index_pt, label="Pinch...",
            )
        else:
            # Pinch released.
            if self._pinch_active:
                was_dragging = self._dragging
                held = now - self._pinch_start_time
                self._pinch_active = False
                self._dragging = False

                if was_dragging:
                    return GestureResult(Gesture.DRAG_END, label="Drop")

                # Quick pinch => click. Decide single vs double.
                if held < self._cfg.drag_hold_time:
                    return self._register_click(now, index_pt)

        # --- 2. Right click: thumb + middle finger pinch ----------------
        if middle_pinch < self._cfg.right_click_threshold and not index:
            if now - self._last_right_click_time > self._cfg.click_cooldown:
                self._last_right_click_time = now
                return GestureResult(
                    Gesture.RIGHT_CLICK, cursor_point=index_pt,
                    label="Right click",
                )
            return GestureResult(Gesture.NEUTRAL, label="...")

        # --- 3. Scroll: index + middle up together ----------------------
        if index and middle and not ring and not pinky:
            cur_y = index_pt[1]
            if self._scroll_anchor_y is None:
                self._scroll_anchor_y = cur_y
            delta = self._scroll_anchor_y - cur_y
            if abs(delta) > self._cfg.scroll_deadzone:
                direction = 1 if delta > 0 else -1
                self._scroll_anchor_y = cur_y
                return GestureResult(
                    Gesture.SCROLL,
                    scroll_amount=direction * self._cfg.scroll_speed,
                    label="Scroll " + ("up" if direction > 0 else "down"),
                )
            return GestureResult(Gesture.NEUTRAL, label="Scroll ready")
        else:
            self._scroll_anchor_y = None

        # --- 4. Move: index finger extended -----------------------------
        if index:
            return GestureResult(
                Gesture.MOVE, cursor_point=index_pt, label="Move",
            )

        # --- 5. Neutral --------------------------------------------------
        return GestureResult(Gesture.NEUTRAL, label="Neutral")

    # ------------------------------------------------------------------ #
    def poll_pending_click(self) -> Optional[GestureResult]:
        """Emit a delayed single click once the double-click window lapses.

        A single pinch could still become a double click, so the single
        click is held back briefly. Call this every frame to flush it.
        """
        if self._pending_single_click_time == 0.0:
            return None
        now = time.time()
        if now - self._pending_single_click_time >= self._cfg.double_click_window:
            self._pending_single_click_time = 0.0
            return GestureResult(Gesture.LEFT_CLICK, label="Left click")
        return None

    # ------------------------------------------------------------------ #
    def _register_click(self, now: float, cursor_pt) -> GestureResult:
        """Decide whether a pinch is the first or second of a double click."""
        if now - self._last_click_time <= self._cfg.double_click_window:
            # Second pinch within the window -> double click.
            self._last_click_time = 0.0
            self._pending_single_click_time = 0.0
            return GestureResult(
                Gesture.DOUBLE_CLICK, cursor_point=cursor_pt,
                label="Double click",
            )

        # First pinch: start the double-click window. Hold the single click
        # back until the window elapses (flushed by poll_pending_click).
        self._last_click_time = now
        self._pending_single_click_time = now
        return GestureResult(
            Gesture.MOVE, cursor_point=cursor_pt, label="Click?",
        )

    def _reset_transient_state(self) -> None:
        self._scroll_anchor_y = None
        self._pinch_active = False
