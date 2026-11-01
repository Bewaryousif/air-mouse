"""
Configuration for the Air Mouse application.

All tunable parameters live here so behaviour can be adjusted without
touching the core logic. Values can also be overridden at runtime by a
``config.json`` file placed next to the project root (see ``Config.load``).
"""

from __future__ import annotations

import json
import os
from dataclasses import dataclass, asdict, field


@dataclass
class Config:
    # ---- Camera ---------------------------------------------------------
    camera_index: int = 0           # Which webcam to use (0 = default).
    frame_width: int = 640          # Capture width in pixels.
    frame_height: int = 480         # Capture height in pixels.
    flip_horizontal: bool = True    # Mirror the image (natural, selfie-view).

    # ---- Hand detection (MediaPipe) ------------------------------------
    max_hands: int = 1
    detection_confidence: float = 0.7
    tracking_confidence: float = 0.6

    # ---- Cursor movement ------------------------------------------------
    # The active region is a rectangle inside the camera frame. The hand
    # only needs to move inside this smaller box to reach the whole screen,
    # which keeps the gesture comfortable.
    frame_margin: int = 100         # Pixels of margin around the active box.
    smoothing: float = 5.0          # Higher = smoother but more lag.
    move_deadzone: float = 1.5      # Ignore sub-pixel jitter (screen px).

    # ---- Gesture thresholds --------------------------------------------
    # Distances are normalised to the hand size so they work at any
    # distance from the camera.
    pinch_threshold: float = 0.05           # Thumb+index => left click/drag.
    right_click_threshold: float = 0.05     # Thumb+middle => right click.
    double_click_window: float = 0.40       # Seconds between two pinches.
    click_cooldown: float = 0.30            # Min seconds between click events.
    drag_hold_time: float = 0.35            # Hold a pinch this long => drag.

    # ---- Scrolling ------------------------------------------------------
    scroll_speed: int = 40          # Wheel units per scroll step.
    scroll_deadzone: float = 0.04   # Vertical movement before scrolling.

    # ---- UI / display ---------------------------------------------------
    show_window: bool = True
    show_landmarks: bool = True
    window_title: str = "Air Mouse  —  webcam gesture control"

    # ---- Safety ---------------------------------------------------------
    # PyAutoGUI fail-safe: slamming the real cursor into a screen corner
    # aborts the program. Kept on by default as an emergency stop.
    failsafe: bool = True

    _source_path: str = field(default="", repr=False)

    # ------------------------------------------------------------------ #
    @classmethod
    def load(cls, path: str | None = None) -> "Config":
        """Load config, overlaying values from ``config.json`` if present."""
        cfg = cls()
        if path is None:
            here = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            path = os.path.join(here, "config.json")
        if path and os.path.isfile(path):
            try:
                with open(path, "r", encoding="utf-8") as fh:
                    data = json.load(fh)
                known = {f for f in cls.__dataclass_fields__}
                for key, value in data.items():
                    if key in known and not key.startswith("_"):
                        setattr(cfg, key, value)
                cfg._source_path = path
            except (OSError, json.JSONDecodeError) as exc:
                print(f"[config] Could not read {path}: {exc}. Using defaults.")
        return cfg

    def save(self, path: str | None = None) -> None:
        """Write the current config to ``config.json``."""
        path = path or self._source_path
        if not path:
            here = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
            path = os.path.join(here, "config.json")
        data = {k: v for k, v in asdict(self).items() if not k.startswith("_")}
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(data, fh, indent=2)
