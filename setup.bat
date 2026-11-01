@echo off
REM ===================================================================
REM  Air Mouse - one-click setup for Windows
REM
REM  Creates a Python virtual environment and installs all required
REM  libraries. Prefers Python 3.11, falls back to 3.12, then 3.10/3.9.
REM  MediaPipe does NOT support Python 3.13+, so those are rejected.
REM ===================================================================

setlocal enabledelayedexpansion
cd /d "%~dp0"

echo.
echo ==========================================================
echo   Air Mouse  -  Setup
echo ==========================================================
echo.

REM --- Find a compatible Python (3.11 preferred) -------------------
set "PYEXE="
for %%V in (3.11 3.12 3.10 3.9) do (
    if not defined PYEXE (
        py -%%V --version >nul 2>&1
        if !errorlevel! equ 0 (
            set "PYEXE=py -%%V"
            echo [ok] Found Python %%V
        )
    )
)

if not defined PYEXE (
    echo [ERROR] No compatible Python found ^(need 3.9 - 3.12^).
    echo         MediaPipe does not support Python 3.13 or newer.
    echo.
    echo         Please install Python 3.11 from:
    echo         https://www.python.org/downloads/release/python-3119/
    echo         ^(remember to check "Add Python to PATH"^)
    echo.
    pause
    exit /b 1
)

REM --- Create the virtual environment ------------------------------
if exist ".venv" (
    echo [info] Reusing existing .venv
) else (
    echo [..] Creating virtual environment in .venv ...
    %PYEXE% -m venv .venv
    if !errorlevel! neq 0 (
        echo [ERROR] Failed to create the virtual environment.
        pause
        exit /b 1
    )
)

REM --- Install dependencies ----------------------------------------
echo [..] Upgrading pip ...
call ".venv\Scripts\python.exe" -m pip install --upgrade pip

echo [..] Installing dependencies from requirements.txt ...
call ".venv\Scripts\python.exe" -m pip install -r requirements.txt
if !errorlevel! neq 0 (
    echo [ERROR] Dependency installation failed. See the messages above.
    pause
    exit /b 1
)

echo.
echo ==========================================================
echo   Setup complete!  Run the app with:  run.bat
echo ==========================================================
echo.
pause
endlocal
