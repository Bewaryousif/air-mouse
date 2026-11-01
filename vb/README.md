# 🖐️ Air Mouse (VB.NET) — Webcam Gesture Mouse

A **pure Visual Basic .NET** (WinForms) application that turns your webcam into
a mouse. **No Python, no MediaPipe** — it uses [OpenCvSharp](https://github.com/shimat/opencvsharp)
(a .NET wrapper for OpenCV) for webcam capture and hand detection, and the
native Win32 API to drive the cursor.

> Companion to the Python version. This one is 100% .NET so it opens and runs
> straight from Visual Studio with no interpreter setup.

---

## ✨ Gesture controls

Because classic computer-vision finger detection is less precise than a deep
learning model, the gestures are deliberately robust and forgiving:

| Gesture | Action |
| --- | --- |
| ☝️ **One finger** | Move the cursor |
| ✋ **Hold the cursor still ~1 s** | Left click (dwell‑click) |
| ✌️ **Two fingers** | Move without clicking (safe reposition) |
| ✊ **Fist (0 fingers)** | Right click |
| 🖐️ **Open palm (5 fingers)** | Click‑and‑drag |
| 🤟 **Three fingers**, move up/down | Scroll |

The main window shows a live camera preview with the detected hand, the active
region, the current gesture, and FPS. You can tune cursor smoothing and the
dwell‑click time with sliders, switch cameras, toggle the skin‑detection mask,
and disable mouse control to preview safely.

---

## 🧱 Project structure

```
AirMouseVB.sln                 Visual Studio solution
AirMouseVB/
  AirMouseVB.vbproj            VB.NET WinForms project (.NET 8, x64)
  ApplicationEvents.vb         App startup (MainForm as entry window)
  MainForm.vb                  Capture loop, overlay, UI wiring
  MainForm.Designer.vb         WinForms designer layout
  HandDetector.vb              OpenCV skin + contour + finger counting
  GestureEngine.vb             Finger count + dwell -> actions
  MouseController.vb           Win32 SendInput cursor + clicks
```

---

## 🚀 Getting started (Windows)

### Requirements
- **Windows 10/11 (64‑bit)** — OpenCvSharp native binaries are Windows x64.
- **Visual Studio 2022** with the **.NET desktop development** workload, or the
  **.NET 8 SDK**.
- A **webcam**.

### Run in Visual Studio
1. Open **`AirMouseVB.sln`**.
2. Visual Studio restores the NuGet packages automatically
   (`OpenCvSharp4`, `OpenCvSharp4.runtime.win`, `OpenCvSharp4.Extensions`).
3. Press **F5**.
4. Click **▶ Start**, show your hand to the camera, and control the mouse.

### Run from the command line
```bat
cd AirMouseVB
dotnet run
```

---

## 💡 Tips for good detection

Classic skin‑colour detection works best when:
- The lighting is even (avoid strong backlight).
- The background is not skin‑coloured (a plain, contrasting background helps).
- Your hand fills a reasonable part of the frame (not too far away).

Use the **"Show skin‑detection mask"** checkbox to see exactly what the app
detects as skin, and adjust your lighting/background until your hand shows up
as a clean white blob.

---

## ⚙️ Settings (in the window)

| Control | Effect |
| --- | --- |
| **Camera** | Pick which webcam to use. |
| **Control the mouse** | Uncheck to preview gestures without moving the cursor. |
| **Show skin‑detection mask** | See the raw detection mask. |
| **Cursor smoothing** | Higher = smoother but more lag. |
| **Dwell‑click time** | How long to hold still before a click fires. |

---

## 📝 Notes & limitations

- This pure‑CV approach tracks the **whole hand** and counts fingers; it is
  intentionally simpler than the MediaPipe (Python) version. For the most
  precise thumb/finger pinch gestures, see the Python project.
- The app uses the **primary monitor** resolution for cursor mapping.
- Native OpenCV binaries are **Windows x64** only; the project targets x64.

## 📄 License

MIT.
