# Start MCP Auto-Shutdown Monitor
# Manually starts the monitor that will shutdown MCP servers when Cursor exits

Write-Host "🚀 Starting MCP Auto-Shutdown Monitor..." -ForegroundColor Green
Write-Host "This will monitor Cursor and automatically stop MCP servers (ports 3000, 3001) when Cursor exits" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop the monitor" -ForegroundColor Cyan

# Start the auto-shutdown script in the background
$scriptPath = Join-Path $PSScriptRoot "auto-shutdown-mcp.ps1"
if (Test-Path $scriptPath) {
    Start-Process PowerShell -ArgumentList "-ExecutionPolicy Bypass -File `"$scriptPath`" -Verbose" -WindowStyle Minimized
    Write-Host "✅ Auto-shutdown monitor started in background" -ForegroundColor Green
    Write-Host "Monitor will automatically stop MCP servers when you close Cursor" -ForegroundColor Green
} else {
    Write-Host "❌ Error: auto-shutdown-mcp.ps1 not found" -ForegroundColor Red
} 