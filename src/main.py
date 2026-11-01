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

# Config is lightweight and has no third-party dependencies.
from config import Config


# Map the third-party modules we need to the friendly package names a user
# would install, so a missing dependency gives a clear, actionable message.
_REQUIRED_PACKAGES = {
    "cv2": "opencv-python",
    "mediapipe": "mediapipe",
    "pyautogui": "pyautogui",
    "numpy": "numpy",
}


def _check_dependencies() -> list[str]:
    """Return the pip names of any required packages that are missing."""
    import importlib.util

    missing = []
    for module_name, pip_name in _REQUIRED_PACKAGES.items():
        if importlib.util.find_spec(module_name) is None:
            missing.append(pip_name)
    return missing


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

    # Fail early with a clear message if the libraries are not installed,
    # instead of a cryptic ModuleNotFoundError / "Exception Thrown: 2".
    missing = _check_dependencies()
    if missing:
        print("=" * 64, file=sys.stderr)
        print(" Air Mouse cannot start: required packages are missing.",
              file=sys.stderr)
        print("=" * 64, file=sys.stderr)
        print(" Missing: " + ", ".join(missing), file=sys.stderr)
        print("", file=sys.stderr)
        print(" Fix it one of these ways:", file=sys.stderr)
        print("   * Double-click  setup.bat   (installs everything), then run.bat",
              file=sys.stderr)
        print("   * Or run:  pip install -r requirements.txt", file=sys.stderr)
        print("   * In Visual Studio: click 'Install packages' in the yellow",
              file=sys.stderr)
        print("     bar, OR Tools > Python > Python Environments > Packages.",
              file=sys.stderr)
        print("", file=sys.stderr)
        print(" Note: use Python 3.9-3.12 (3.11 recommended). MediaPipe does",
              file=sys.stderr)
        print(" NOT support Python 3.13 or newer.", file=sys.stderr)
        print("=" * 64, file=sys.stderr)
        return 3

    # Import the app only after dependencies are confirmed present.
    from air_mouse_app import AirMouseApp

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
