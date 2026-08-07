@echo off
setlocal enabledelayedexpansion

:: Initialize option flags to false
set "DEBUG_MODE=false"
set "BUILD_MODE=false"
set "LAUNCH_ARG="
set "TARGET_PATH="
set "SCRIPT_DIR=%~dp0"

:: Loop through all command line arguments
:parse_args
if "%~1"=="" goto :args_done
if /i "%~1"=="-Debug" (
    set "DEBUG_MODE=true"
    shift
    goto :parse_args
)
if /i "%~1"=="-Build" (
    set "BUILD_MODE=true"
    set "LAUNCH_ARG=-NoLaunch"
    shift
    goto :parse_args
)
if /i "%~1"=="-TargetPath" (
    set "TARGET_PATH=%~2"
    shift
    shift
    goto :parse_args
)

:: Handle unexpected arguments
echo [ERROR] Unknown parameter: %~1
echo Usage: %~nx0 -TargetPath <path> [-Debug\|-Build]
exit /b 1

:args_done

:: Validate that the required TargetPath parameter is present
if "%TARGET_PATH%"=="" (
    echo [ERROR] Missing required parameter: -TargetPath
    echo Usage: %~nx0 -TargetPath <path> [-Debug\|-Build]
    exit /b 1
)

:: Check that both options are not set simultaneously
if "%DEBUG_MODE%"=="true" if "%BUILD_MODE%"=="true" (
    echo [ERROR] Cannot set both -Debug and -Build at the same time.
    echo Usage: %~nx0 -TargetPath <path> [-Debug\|-Build]
    exit /b 1
)

:: --- YOUR SCRIPT LOGIC GOES HERE ---

"C:\Users\scott.TOWEL42\AppData\Local\Microsoft\WindowsApps\pwsh.exe" -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%InstallAndLaunch.ps1" -TargetPath "%TARGET_PATH%" -NoColor %LAUNCH_ARG% -EmbyRoot "C:\Users\scott.TOWEL42\Dropbox\home\bin\EmbyServer"


endlocal
pause