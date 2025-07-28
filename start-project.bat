@echo off
echo Starting WindowsAppsManager + MCP Toolkit...
echo.

REM Start the project with MCP servers
powershell -ExecutionPolicy Bypass -File "start-project-with-mcp.ps1"

echo.
echo Project startup complete!
pause 