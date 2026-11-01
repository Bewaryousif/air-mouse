"""
On-screen overlay drawn onto the webcam preview window.

Shows the active control region, the current gesture, FPS, and a short
legend of the gesture controls so the user always knows what to do.
"""

from __future__ import annotations

import cv2

from config import Config

_PANEL_BG = (32, 32, 32)
_ACCENT = (0, 200, 255)
_TEXT = (240, 240, 240)
_OK = (90, 220, 120)

_LEGEND = [
    "Index up .......... move",
    "Thumb+Index ....... click / 2x = double",
    "Thumb+Middle ...... right click",
    "Hold pinch ........ drag",
    "Index+Middle up ... scroll",
    "Q / ESC ........... quit",
]


class UIOverlay:
    def __init__(self, config: Config):
        self._cfg = config
        self._font = cv2.FONT_HERSHEY_SIMPLEX

    def draw(self, frame, gesture_label: str, fps: float, hand_found: bool):
        h, w = frame.shape[:2]
        m = self._cfg.frame_margin

        # Active control region rectangle.
        colour = _OK if hand_found else _ACCENT
        cv2.rectangle(frame, (m, m), (w - m, h - m), colour, 2)
        cv2.putText(frame, "active region", (m + 6, m + 20),
                    self._font, 0.5, colour, 1, cv2.LINE_AA)

        # Top status bar.
        cv2.rectangle(frame, (0, 0), (w, 34), _PANEL_BG, -1)
        status = f"Gesture: {gesture_label}"
        cv2.putText(frame, status, (10, 23), self._font, 0.6, _TEXT, 2,
                    cv2.LINE_AA)
        fps_text = f"{fps:4.1f} FPS"
        cv2.putText(frame, fps_text, (w - 110, 23), self._font, 0.6,
                    _ACCENT, 2, cv2.LINE_AA)

        # Legend panel (bottom-left).
        panel_h = 18 * len(_LEGEND) + 14
        y0 = h - panel_h
        overlay = frame.copy()
        cv2.rectangle(overlay, (0, y0), (260, h), _PANEL_BG, -1)
        cv2.addWeighted(overlay, 0.65, frame, 0.35, 0, frame)
        for i, line in enumerate(_LEGEND):
            cv2.putText(frame, line, (10, y0 + 22 + i * 18),
                        self._font, 0.42, _TEXT, 1, cv2.LINE_AA)

        return frame
