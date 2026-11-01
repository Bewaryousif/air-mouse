"""
Hand tracking built on top of MediaPipe Hands.

This module is deliberately thin: it wraps the MediaPipe solution and
exposes a clean, typed result (``HandData``) so the rest of the app never
has to touch MediaPipe internals directly.
"""

from __future__ import annotations

import math
from dataclasses import dataclass
from typing import List, Optional, Tuple

import cv2
import mediapipe as mp

from config import Config


# MediaPipe landmark indices for the finger tips and relevant joints.
WRIST = 0
THUMB_TIP = 4
THUMB_IP = 3
INDEX_TIP = 8
INDEX_PIP = 6
INDEX_MCP = 5
MIDDLE_TIP = 12
MIDDLE_PIP = 10
RING_TIP = 16
RING_PIP = 14
PINKY_TIP = 20
PINKY_PIP = 18

FINGER_TIPS = (THUMB_TIP, INDEX_TIP, MIDDLE_TIP, RING_TIP, PINKY_TIP)


@dataclass
class HandData:
    """A single detected hand, with helpers used by the gesture layer."""

    # Landmarks normalised to [0, 1] relative to the frame (x, y, z).
    landmarks: List[Tuple[float, float, float]]
    # Landmarks in pixel coordinates (x, y) for drawing.
    pixels: List[Tuple[int, int]]
    handedness: str          # "Left" or "Right"
    frame_width: int
    frame_height: int

    def point(self, idx: int) -> Tuple[float, float]:
        """Normalised (x, y) of a landmark."""
        lm = self.landmarks[idx]
        return lm[0], lm[1]

    def pixel(self, idx: int) -> Tuple[int, int]:
        """Pixel (x, y) of a landmark."""
        return self.pixels[idx]

    @property
    def hand_scale(self) -> float:
        """A size reference (wrist -> index MCP) to normalise distances."""
        return max(self.distance(WRIST, INDEX_MCP), 1e-6)

    def distance(self, a: int, b: int) -> float:
        """Euclidean distance between two landmarks in normalised space."""
        ax, ay = self.point(a)
        bx, by = self.point(b)
        return math.hypot(ax - bx, ay - by)

    def normalised_distance(self, a: int, b: int) -> float:
        """Distance between two landmarks, scaled by hand size.

        Dividing by the hand size makes thresholds independent of how far
        the hand is from the camera.
        """
        return self.distance(a, b) / self.hand_scale

    def fingers_up(self) -> List[bool]:
        """Return [thumb, index, middle, ring, pinky] extension flags."""
        up: List[bool] = []

        # Thumb: compare horizontally, accounting for handedness.
        thumb_tip_x = self.point(THUMB_TIP)[0]
        thumb_ip_x = self.point(THUMB_IP)[0]
        if self.handedness == "Right":
            up.append(thumb_tip_x < thumb_ip_x)
        else:
            up.append(thumb_tip_x > thumb_ip_x)

        # Other fingers: tip above its PIP joint (smaller y = higher).
        for tip, pip in (
            (INDEX_TIP, INDEX_PIP),
            (MIDDLE_TIP, MIDDLE_PIP),
            (RING_TIP, RING_PIP),
            (PINKY_TIP, PINKY_PIP),
        ):
            up.append(self.point(tip)[1] < self.point(pip)[1])
        return up


class HandTracker:
    """Wraps MediaPipe Hands and returns :class:`HandData` per frame."""

    def __init__(self, config: Config):
        self._config = config
        self._mp_hands = mp.solutions.hands
        self._mp_draw = mp.solutions.drawing_utils
        self._mp_styles = mp.solutions.drawing_styles
        self._hands = self._mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=config.max_hands,
            min_detection_confidence=config.detection_confidence,
            min_tracking_confidence=config.tracking_confidence,
        )

    def process(self, frame_bgr) -> Optional[HandData]:
        """Detect the primary hand in a BGR frame."""
        h, w = frame_bgr.shape[:2]
        rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        rgb.flags.writeable = False
        result = self._hands.process(rgb)

        if not result.multi_hand_landmarks:
            return None

        hand_landmarks = result.multi_hand_landmarks[0]
        handedness = "Right"
        if result.multi_handedness:
            handedness = result.multi_handedness[0].classification[0].label

        landmarks = [(lm.x, lm.y, lm.z) for lm in hand_landmarks.landmark]
        pixels = [(int(lm.x * w), int(lm.y * h)) for lm in hand_landmarks.landmark]

        return HandData(
            landmarks=landmarks,
            pixels=pixels,
            handedness=handedness,
            frame_width=w,
            frame_height=h,
        )

    def draw(self, frame_bgr, raw_result=None) -> None:
        """Draw landmarks for the most recent detection (optional helper)."""
        # Kept simple: the app draws its own overlay. This is provided for
        # debugging if a caller wants the full MediaPipe skeleton.
        pass

    def draw_landmarks(self, frame_bgr, hand: HandData) -> None:
        """Draw a lightweight skeleton from HandData pixel coordinates."""
        connections = self._mp_hands.HAND_CONNECTIONS
        for a, b in connections:
            pa, pb = hand.pixels[a], hand.pixels[b]
            cv2.line(frame_bgr, pa, pb, (80, 220, 120), 2)
        for px, py in hand.pixels:
            cv2.circle(frame_bgr, (px, py), 3, (0, 180, 255), -1)

    def close(self) -> None:
        self._hands.close()
