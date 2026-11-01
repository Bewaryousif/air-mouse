"""
Air Mouse — webcam-based virtual mouse controlled with hand gestures.

Entry point. Run from Visual Studio (this file is the project's start-up
file) or from a terminal:

    python src/main.py
    python src/main.py --camera 1 --no-window

Gesture controls
----------------
    * Index finger up ............... move the cursor
    * Pinch thumb + index ........... left click  (two quick pinches = double)
    * Pinch thumb + middle .......... right click
    * Hold the thumb+index pinch .... click-and-drag
    * Index + middle up, move up/dn . scroll
    * Press Q or ESC ................ quit
"""

from __future__ import annotations

import argparse
import sys

from config import Config
from air_mouse_app import AirMouseApp


def parse_args(argv=None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        prog="air-mouse",
        description="Control your PC mouse with webcam hand gestures.",
    )
    parser.add_argument("--camera", type=int, default=None,
                        help="Webcam index to use (default: 0).")
    parser.add_argument("--no-window", action="store_true",
                        help="Run without the camera preview window.")
    parser.add_argument("--smoothing", type=float, default=None,
                        help="Cursor smoothing factor (higher = smoother).")
    parser.add_argument("--config", type=str, default=None,
                        help="Path to a config.json file.")
    return parser.parse_args(argv)


def main(argv=None) -> int:
    args = parse_args(argv)
    config = Config.load(args.config)

    if args.camera is not None:
        config.camera_index = args.camera
    if args.no_window:
        config.show_window = False
    if args.smoothing is not None:
        config.smoothing = args.smoothing

    try:
        app = AirMouseApp(config)
        app.run()
    except RuntimeError as exc:
        print(f"[error] {exc}", file=sys.stderr)
        return 1
    except Exception as exc:  # pragma: no cover - top-level safety net
        print(f"[fatal] Unexpected error: {exc}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
