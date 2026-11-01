@echo off
REM ===================================================================
REM  Air Mouse - launcher for Windows
REM  Runs the app using the virtual environment created by setup.bat.
REM  Any arguments are passed straight through, e.g.:  run.bat --camera 1
REM ===================================================================

cd /d "%~dp0"

if not exist ".venv\Scripts\python.exe" (
    echo [ERROR] Virtual environment not found.
    echo         Please run  setup.bat  first.
    echo.
    pause
    exit /b 1
)

echo Starting Air Mouse...  (press Q or ESC in the window to quit)
echo.
call ".venv\Scripts\python.exe" src\main.py %*

if %errorlevel% neq 0 (
    echo.
    echo [warn] Air Mouse exited with an error. See the messages above.
    pause
)
