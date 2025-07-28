# Start Project with MCP Server Toolkit
# Automatically starts the MCP server toolkit when this project is opened
param(
    [switch]$Verbose = $false,
    [switch]$SkipMCP = $false,
    [switch]$Background = $true
)

# Configuration
$PROJECT_NAME = "WindowsAppsManager + MCP Toolkit"
$MCP_STARTUP_SCRIPT = "start-mcp-servers.ps1"
$AUTO_SHUTDOWN_SCRIPT = "start-auto-shutdown-monitor.ps1"
$LOG_FILE = "project-startup.log"

function Write-ProjectLog {
    param([string]$Message, [string]$Type = "Info")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Type] $Message"
    
    $color = switch ($Type) {
        "Success" { "Green" }
        "Error" { "Red" }
        "Warning" { "Yellow" }
        "MCP" { "Cyan" }
        default { "White" }
    }
    
    Write-Host $logMessage -ForegroundColor $color
    Add-Content -Path $LOG_FILE -Value $logMessage
}

function Test-MCPServersRunning {
    $running = $false
    foreach ($port in @(3000, 3001)) {
        try {
            $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | 
                         Where-Object { $_.State -eq "Listen" }
            if ($connection) {
                $running = $true
                break
            }
        }
        catch {
            # Port not in use
        }
    }
    return $running
}

function Start-MCPServers {
    Write-ProjectLog "Starting MCP Server Toolkit..." "MCP"
    
    if (-not (Test-Path $MCP_STARTUP_SCRIPT)) {
        Write-ProjectLog "Error: MCP startup script not found: $MCP_STARTUP_SCRIPT" "Error"
        return $false
    }
    
    try {
        if ($Background) {
            # Start MCP servers in background
            Start-Process PowerShell -ArgumentList "-ExecutionPolicy Bypass -File `"$MCP_STARTUP_SCRIPT`"" -WindowStyle Minimized
            Write-ProjectLog "MCP servers starting in background..." "MCP"
        } else {
            # Start MCP servers in foreground
            & ".\$MCP_STARTUP_SCRIPT"
        }
        
        # Wait a moment for servers to start
        Start-Sleep -Seconds 3
        
        # Check if servers are running
        $attempts = 0
        $maxAttempts = 10
        
        while ($attempts -lt $maxAttempts) {
            if (Test-MCPServersRunning) {
                Write-ProjectLog "MCP servers are running on ports 3000 and 3001" "Success"
                return $true
            }
            
            $attempts++
            Write-ProjectLog "Waiting for MCP servers to start... (attempt $attempts/$maxAttempts)" "MCP"
            Start-Sleep -Seconds 2
        }
        
        Write-ProjectLog "MCP servers may not have started properly" "Warning"
        return $false
    }
    catch {
        Write-ProjectLog "Error starting MCP servers: $($_.Exception.Message)" "Error"
        return $false
    }
}

function Start-AutoShutdownMonitor {
    Write-ProjectLog "Starting auto-shutdown monitor..." "MCP"
    
    if (-not (Test-Path $AUTO_SHUTDOWN_SCRIPT)) {
        Write-ProjectLog "Warning: Auto-shutdown script not found: $AUTO_SHUTDOWN_SCRIPT" "Warning"
        return $false
    }
    
    try {
        # Start auto-shutdown monitor in background
        Start-Process PowerShell -ArgumentList "-ExecutionPolicy Bypass -File `"$AUTO_SHUTDOWN_SCRIPT`"" -WindowStyle Minimized
        Write-ProjectLog "Auto-shutdown monitor started" "Success"
        return $true
    }
    catch {
        Write-ProjectLog "Error starting auto-shutdown monitor: $($_.Exception.Message)" "Error"
        return $false
    }
}

function Show-ProjectStatus {
    Write-ProjectLog "=== $PROJECT_NAME Status ===" "Info"
    
    # Check MCP servers
    $mcpRunning = Test-MCPServersRunning
    Write-ProjectLog "MCP Servers: $(if ($mcpRunning) { 'Running' } else { 'Not Running' })" "Info"
    
    # Check ports
    foreach ($port in @(3000, 3001)) {
        try {
            $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | 
                         Where-Object { $_.State -eq "Listen" }
            if ($connection) {
                Write-ProjectLog "Port $port: Active" "Success"
            } else {
                Write-ProjectLog "Port $port: Not Active" "Warning"
            }
        }
        catch {
            Write-ProjectLog "Port $port: Error checking" "Error"
        }
    }
    
    # Show access URLs
    if ($mcpRunning) {
        Write-ProjectLog "Access URLs:" "Info"
        Write-ProjectLog "   Web Interface: http://localhost:3000" "MCP"
        Write-ProjectLog "   Dashboard: http://localhost:3001" "MCP"
    }
}

# Main execution
Write-ProjectLog "Starting $PROJECT_NAME" "Info"
Write-ProjectLog "Project directory: $PWD" "Info"

# Check if MCP servers are already running
if (Test-MCPServersRunning) {
    Write-ProjectLog "MCP servers are already running" "Success"
    Show-ProjectStatus
} else {
    if (-not $SkipMCP) {
        # Start MCP servers
        $mcpStarted = Start-MCPServers
        
        if ($mcpStarted) {
            # Start auto-shutdown monitor
            Start-AutoShutdownMonitor
        }
    } else {
        Write-ProjectLog "Skipping MCP startup (SkipMCP flag set)" "Warning"
    }
}

# Show final status
Show-ProjectStatus

Write-ProjectLog "Project startup complete!" "Success"
Write-ProjectLog "Log file: $LOG_FILE" "Info" 