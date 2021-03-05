@ECHO OFF

net session >nul 2>&1
IF NOT %ERRORLEVEL% EQU 0 (
   ECHO ERROR: Please run Bat as Administrator.
   PAUSE
   EXIT /B 1
)

@SETLOCAL enableextensions
@CD /d "%~dp0"

REM The following directory is for .NET 4
SET DOTNETFX4=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727
SET PATH=%PATH%;%DOTNETFX4%

ECHO Installing MyServer...
ECHO ---------------------------------------------------
InstallUtil /u /i .\MyServer.exe
ECHO ---------------------------------------------------
ECHO Done.
PAUSE