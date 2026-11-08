@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Make-Class-Diagram.ps1"
if errorlevel 1 (
    echo.
    echo The diagram was not created. Read the message above.
    pause
    exit /b 1
)
echo.
echo Saved ClassDiagram.svg in this folder.
echo The picture should be open. Put that file in your report.
pause
