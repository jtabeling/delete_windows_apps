@echo off
echo MCP Toolkit Copy Script
echo =====================
echo.

if "%~1"=="" (
    echo Usage: copy-mcp-toolkit.bat [target-project-path] [options]
    echo.
    echo Examples:
    echo   copy-mcp-toolkit.bat "H:\Cursor\my_new_project"
    echo   copy-mcp-toolkit.bat "H:\Cursor\my_new_project" -Minimal
    echo   copy-mcp-toolkit.bat "H:\Cursor\my_new_project" -SkipVerification
    echo.
    echo Options:
    echo   -Minimal          Copy only core MCP functionality
    echo   -SkipVerification Skip installation verification
    echo   -DetailedLogging  Enable detailed logging output
    echo.
    pause
    exit /b 1
)

echo Target project: %1
echo.

REM Run the PowerShell script
powershell -ExecutionPolicy Bypass -File "copy-mcp-toolkit-to-project.ps1" %*

echo.
echo Copy process complete!
pause 