# Auto-Shutdown MCP Server Toolkit
# Monitors Cursor process and automatically stops MCP servers when Cursor exits
param(
    [switch]$Verbose = $false,
    [switch]$Background = $true
)

# Configuration
$CURSOR_PROCESS_NAME = "Cursor"
$MCP_PORTS = @(3000, 3001)
$CHECK_INTERVAL = 5  # seconds
$LOG_FILE = "mcp-auto-shutdown.log"

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    Write-Host $logMessage
    Add-Content -Path $LOG_FILE -Value $logMessage
}

function Stop-MCPServers {
    Write-Log "Stopping MCP servers..."
    
    # Stop processes on specific ports
    foreach ($port in $MCP_PORTS) {
        try {
            $processes = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | 
                        Where-Object { $_.State -eq "Listen" } | 
                        ForEach-Object { Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue }
            
            foreach ($process in $processes) {
                if ($process) {
                    Write-Log "Stopping process $($process.Name) (PID: $($process.Id)) on port $port"
                    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                }
            }
        }
        catch {
            Write-Log "Error stopping processes on port $port : $($_.Exception.Message)"
        }
    }
    
    # Also stop any remaining Node.js processes that might be MCP servers
    try {
        $nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
        foreach ($process in $nodeProcesses) {
            Write-Log "Stopping Node.js process (PID: $($process.Id))"
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Log "Error stopping Node.js processes: $($_.Exception.Message)"
    }
    
    Write-Log "MCP servers stopped"
}

function Test-CursorRunning {
    try {
        $cursorProcess = Get-Process -Name $CURSOR_PROCESS_NAME -ErrorAction SilentlyContinue
        return $cursorProcess -ne $null
    }
    catch {
        return $false
    }
}

function Test-MCPServersRunning {
    $running = $false
    foreach ($port in $MCP_PORTS) {
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

# Main monitoring loop
Write-Log "Starting MCP auto-shutdown monitor for Cursor"
Write-Log "Monitoring for Cursor process: $CURSOR_PROCESS_NAME"
Write-Log "Will stop MCP servers on ports: $($MCP_PORTS -join ', ')"
Write-Log "Check interval: $CHECK_INTERVAL seconds"

$cursorWasRunning = Test-CursorRunning
Write-Log "Initial Cursor status: $(if ($cursorWasRunning) { 'Running' } else { 'Not Running' })"

try {
    while ($true) {
        $cursorRunning = Test-CursorRunning
        $mcpRunning = Test-MCPServersRunning
        
        if ($Verbose) {
            Write-Log "Cursor: $(if ($cursorRunning) { 'Running' } else { 'Not Running' }), MCP: $(if ($mcpRunning) { 'Running' } else { 'Not Running' })"
        }
        
        # If Cursor was running but now it's not, and MCP servers are running, stop them
        if (-not $cursorRunning -and $cursorWasRunning -and $mcpRunning) {
            Write-Log "Cursor has exited. Stopping MCP servers..."
            Stop-MCPServers
            Write-Log "Auto-shutdown complete. Exiting monitor."
            break
        }
        
        $cursorWasRunning = $cursorRunning
        Start-Sleep -Seconds $CHECK_INTERVAL
    }
}
catch {
    Write-Log "Error in monitoring loop: $($_.Exception.Message)"
    Stop-MCPServers
}
finally {
    Write-Log "MCP auto-shutdown monitor stopped"
} 