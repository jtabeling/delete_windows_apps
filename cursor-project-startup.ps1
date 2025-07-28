# Cursor Project Startup - MCP Server Toolkit
# This script starts the MCP server toolkit when the project is opened in Cursor

# Configuration
$PROJECT_ROOT = $PWD
$MCP_STARTUP = "start-mcp-servers.ps1"
$AUTO_SHUTDOWN = "start-auto-shutdown-monitor.ps1"

Write-Host "Cursor Project: Starting MCP Server Toolkit..." -ForegroundColor Green

# Check if MCP servers are already running
$mcpRunning = $false
foreach ($port in @(3000, 3001)) {
    try {
        $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | 
                     Where-Object { $_.State -eq "Listen" }
        if ($connection) {
            $mcpRunning = $true
            break
        }
    }
    catch { }
}

if ($mcpRunning) {
    Write-Host "MCP servers are already running on ports 3000 and 3001" -ForegroundColor Yellow
    Write-Host "Web Interface: http://localhost:3000" -ForegroundColor Cyan
    Write-Host "Dashboard: http://localhost:3001" -ForegroundColor Cyan
} else {
    # Start MCP servers
    if (Test-Path $MCP_STARTUP) {
        Write-Host "Starting MCP servers..." -ForegroundColor Green
        Start-Process PowerShell -ArgumentList "-ExecutionPolicy Bypass -File `"$MCP_STARTUP`"" -WindowStyle Minimized
        
        # Wait for servers to start
        Start-Sleep -Seconds 5
        
        # Check if servers started successfully
        $started = $false
        foreach ($port in @(3000, 3001)) {
            try {
                $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | 
                             Where-Object { $_.State -eq "Listen" }
                if ($connection) {
                    $started = $true
                }
            }
            catch { }
        }
        
        if ($started) {
            Write-Host "MCP servers started successfully!" -ForegroundColor Green
            Write-Host "Web Interface: http://localhost:3000" -ForegroundColor Cyan
            Write-Host "Dashboard: http://localhost:3001" -ForegroundColor Cyan
            
            # Start auto-shutdown monitor
            if (Test-Path $AUTO_SHUTDOWN) {
                Start-Process PowerShell -ArgumentList "-ExecutionPolicy Bypass -File `"$AUTO_SHUTDOWN`"" -WindowStyle Minimized
                Write-Host "Auto-shutdown monitor started" -ForegroundColor Green
            }
        } else {
            Write-Host "Warning: MCP servers may not have started properly" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Error: MCP startup script not found" -ForegroundColor Red
    }
}

Write-Host "Project startup complete!" -ForegroundColor Green 