"""
The Air Mouse application orchestrator.

Owns the main capture loop: read a webcam frame, detect the hand, recognise
a gesture, drive the OS mouse, and render the preview overlay.
"""

from __future__ import annotations

import time

import cv2

from config import Config
from hand_tracker import HandTracker
from gesture_recognizer import GestureRecognizer
from mouse_controller import MouseController
from ui_overlay import UIOverlay


class AirMouseApp:
    def __init__(self, config: Config | None = None):
        self.config = config or Config.load()
        self._tracker = HandTracker(self.config)
        self._recognizer = GestureRecognizer(self.config)
        self._mouse = MouseController(self.config)
        self._overlay = UIOverlay(self.config)

        self._cap: cv2.VideoCapture | None = None
        self._running = False
        self._last_label = "Starting..."

    # ------------------------------------------------------------------ #
    def _open_camera(self) -> cv2.VideoCapture:
        cap = cv2.VideoCapture(self.config.camera_index)
        cap.set(cv2.CAP_PROP_FRAME_WIDTH, self.config.frame_width)
        cap.set(cv2.CAP_PROP_FRAME_HEIGHT, self.config.frame_height)
        if not cap.isOpened():
            raise RuntimeError(
                f"Could not open webcam index {self.config.camera_index}. "
                "Check the camera is connected and not in use by another app."
            )
        return cap

    # ------------------------------------------------------------------ #
    def run(self) -> None:
        """Start the air mouse. Blocks until the user quits."""
        self._cap = self._open_camera()
        self._running = True

        print("Air Mouse is running.")
        print(f"Screen resolution: {self._mouse.screen_w}x{self._mouse.screen_h}")
        print("Press Q or ESC in the preview window to quit.")

        prev_t = time.time()
        fps = 0.0

        try:
            while self._running:
                ok, frame = self._cap.read()
                if not ok:
                    print("[warn] Dropped a frame from the camera.")
                    continue

                if self.config.flip_horizontal:
                    frame = cv2.flip(frame, 1)

                hand = self._tracker.process(frame)

                # Recognise the active gesture and act on it.
                result = self._recognizer.update(hand)
                self._mouse.apply(result)
                if result.label:
                    self._last_label = result.label

                # Flush any delayed single click (double-click resolution).
                pending = self._recognizer.poll_pending_click()
                if pending is not None:
                    self._mouse.apply(pending)
                    self._last_label = pending.label

                # Render preview.
                if self.config.show_window:
                    if hand is not None and self.config.show_landmarks:
                        self._tracker.draw_landmarks(frame, hand)

                    now = time.time()
                    dt = now - prev_t
                    prev_t = now
                    if dt > 0:
                        fps = 0.9 * fps + 0.1 * (1.0 / dt)

                    frame = self._overlay.draw(
                        frame, self._last_label, fps, hand is not None
                    )
                    cv2.imshow(self.config.window_title, frame)

                    key = cv2.waitKey(1) & 0xFF
                    if key in (ord("q"), 27):  # q or ESC
                        self._running = False
        except KeyboardInterrupt:
            print("\nInterrupted.")
        finally:
            self.shutdown()

    # ------------------------------------------------------------------ #
    def shutdown(self) -> None:
        self._running = False
        self._mouse.release()
        if self._cap is not None:
            self._cap.release()
        self._tracker.close()
        cv2.destroyAllWindows()
        print("Air Mouse stopped. Goodbye.")
