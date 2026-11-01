# 🖐️ Air Mouse — Webcam Gesture Mouse Control

A professional, touchless **virtual mouse** for Windows PCs. It uses your
**webcam** and **hand + finger gestures** to fully replace a USB mouse — move
the cursor, left/right click, double‑click, drag, and scroll, all in the air.

Built with **Python**, **OpenCV**, **MediaPipe**, and **PyAutoGUI**, and shipped
as a ready‑to‑open **Visual Studio** project (`AirMouse.sln`).

---

## ✨ Features

| Gesture | Action |
| --- | --- |
| ☝️ **Index finger up**, move your hand | Move the cursor |
| 🤏 **Pinch thumb + index** (quick) | Left click |
| 🤏🤏 **Two quick pinches** | Double click |
| 🤏 **Pinch thumb + middle finger** | Right click |
| ✊ **Hold the thumb + index pinch** | Click‑and‑drag (press & hold) |
| ✌️ **Index + middle up**, move up/down | Scroll |
| ⌨️ **Q / ESC** (in preview window) | Quit |

Additional qualities:

- **Distance‑independent gestures** — thresholds are normalised to hand size,
  so gestures work whether your hand is close to or far from the camera.
- **Smoothed cursor** with an adjustable active region, so a small, comfortable
  hand movement reaches the whole screen.
- **Debounced clicks** with a proper double‑click window (no accidental spam).
- **Live overlay** showing the current gesture, FPS, active region, and a legend.
- **Safety fail‑safe** — slam the real cursor into a screen corner to abort.
- **Fully configurable** via `config.json` or command‑line flags.

---

## 🧱 Project structure

```
AirMouse.sln                 Visual Studio solution
AirMouse.pyproj              Visual Studio Python project
config.json                  Runtime configuration (optional overrides)
requirements.txt             Python dependencies
README.md
src/
  main.py                    Entry point + CLI argument parsing
  config.py                  Typed configuration (dataclass + JSON loader)
  hand_tracker.py            MediaPipe wrapper -> clean HandData
  gesture_recognizer.py      Finger positions -> debounced gestures
  mouse_controller.py        Gestures -> OS mouse actions (PyAutoGUI)
  ui_overlay.py              On‑screen preview overlay
  air_mouse_app.py           Main capture loop / orchestrator
```

Each layer has a single responsibility, which keeps the code easy to read,
test, and extend.

---

## 🚀 Getting started

### Option A — Visual Studio (recommended on Windows)

1. Install **Python 3.11** and the **Python development** workload in the
   Visual Studio Installer.
2. Open **`AirMouse.sln`** in Visual Studio.
3. In **Solution Explorer → Python Environments**, add a 3.11 environment and
   install the packages from `requirements.txt`
   (right‑click the environment → *Install from requirements.txt*).
4. Press **F5** (or Ctrl+F5) to run. The preview window opens and the air
   mouse is live.

### Option B — Command line

```bash
# 1. Create and activate a virtual environment (Python 3.11 recommended)
python -m venv .venv
.venv\Scripts\activate        # Windows
# source .venv/bin/activate   # macOS / Linux

# 2. Install dependencies
pip install -r requirements.txt

# 3. Run
python src/main.py
```

### Command‑line options

```bash
python src/main.py --camera 1          # use a different webcam
python src/main.py --smoothing 8       # smoother (but laggier) cursor
python src/main.py --no-window         # run headless, no preview window
python src/main.py --config my.json    # custom config file
```

---

## ⚙️ Configuration

Edit `config.json` to tune behaviour without changing code. Key settings:

| Key | Meaning |
| --- | --- |
| `camera_index` | Which webcam to use (`0` = default). |
| `smoothing` | Higher = smoother cursor but more lag. |
| `frame_margin` | Size of the active region border; smaller = more sensitive. |
| `pinch_threshold` | How close thumb + index must be to count as a click. |
| `double_click_window` | Max seconds between two pinches for a double click. |
| `drag_hold_time` | Seconds to hold a pinch before it becomes a drag. |
| `scroll_speed` | Wheel units per scroll step. |
| `failsafe` | Keep `true`: cursor into a corner = emergency stop. |

---

## 🧰 Requirements

- **Python 3.9 – 3.11** (MediaPipe support is best on ≤ 3.11).
- A working **webcam**.
- Windows, macOS, or Linux. (Tested primarily on Windows; PyAutoGUI works
  cross‑platform.)

---

## 🛟 Troubleshooting

- **"Could not open webcam"** — another app may be using the camera, or the
  index is wrong. Try `--camera 1`, `--camera 2`, etc.
- **Cursor is jittery** — increase `smoothing` in `config.json`.
- **Clicks trigger too easily / too hard** — adjust `pinch_threshold`.
- **MediaPipe won't install** — ensure you are on Python 3.11 or lower.
- **Cursor won't reach screen edges** — reduce `frame_margin`.

---

## 📄 License

MIT — free to use, modify, and distribute.
