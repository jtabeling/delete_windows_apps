# MCP Server Toolkit Stop Script
# Gracefully stops all MCP servers and web interfaces

param(
    [switch]$Force = $false,
    [switch]$Verbose = $false
)

Write-Host "🛑 Stopping MCP Server Toolkit..." -ForegroundColor Red
Write-Host "================================================" -ForegroundColor Cyan

# Configuration
$WEB_INTERFACE_PORT = 3000
$DASHBOARD_PORT = 3001

# Function to check if a port is in use
function Test-PortInUse {
    param([int]$Port)
    $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    return $connection -ne $null
}

# Function to stop processes by port
function Stop-ProcessByPort {
    param([int]$Port)
    
    try {
        $connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
        foreach ($connection in $connections) {
            $process = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
            if ($process) {
                Write-Host "🛑 Stopping process on port $Port (PID: $($process.Id), Name: $($process.ProcessName))" -ForegroundColor Yellow
                if ($Force) {
                    $process.Kill()
                } else {
                    $process.CloseMainWindow()
                    Start-Sleep -Seconds 2
                    if (-not $process.HasExited) {
                        $process.Kill()
                    }
                }
            }
        }
        return $true
    }
    catch {
        Write-Host "❌ Error stopping processes on port $Port`: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to stop all Node.js processes
function Stop-AllNodeProcesses {
    try {
        $nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
        if ($nodeProcesses) {
            Write-Host "🛑 Found $($nodeProcesses.Count) Node.js processes to stop" -ForegroundColor Yellow
            
            foreach ($process in $nodeProcesses) {
                Write-Host "   Stopping Node.js process (PID: $($process.Id))" -ForegroundColor Gray
                if ($Force) {
                    $process.Kill()
                } else {
                    $process.CloseMainWindow()
                    Start-Sleep -Seconds 1
                    if (-not $process.HasExited) {
                        $process.Kill()
                    }
                }
            }
            
            # Wait for processes to fully stop
            Start-Sleep -Seconds 3
            
            # Check if any processes are still running
            $remainingProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
            if ($remainingProcesses) {
                Write-Host "⚠️  $($remainingProcesses.Count) Node.js processes still running - forcing stop" -ForegroundColor Yellow
                $remainingProcesses | Stop-Process -Force
            }
            
            return $true
        } else {
            Write-Host "ℹ️  No Node.js processes found" -ForegroundColor Gray
            return $true
        }
    }
    catch {
        Write-Host "❌ Error stopping Node.js processes: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Main execution
Write-Host "🔍 Checking current status..." -ForegroundColor Yellow

# Check ports before stopping
$portsInUse = @()
if (Test-PortInUse -Port $WEB_INTERFACE_PORT) {
    $portsInUse += $WEB_INTERFACE_PORT
    Write-Host "⚠️  Port $WEB_INTERFACE_PORT (Web Interface) is in use" -ForegroundColor Yellow
}
if (Test-PortInUse -Port $DASHBOARD_PORT) {
    $portsInUse += $DASHBOARD_PORT
    Write-Host "⚠️  Port $DASHBOARD_PORT (Dashboard) is in use" -ForegroundColor Yellow
}

# Check Node.js processes
$nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
if ($nodeProcesses) {
    Write-Host "📊 Found $($nodeProcesses.Count) Node.js processes running" -ForegroundColor Yellow
    if ($Verbose) {
        foreach ($process in $nodeProcesses) {
            Write-Host "   PID: $($process.Id), Start Time: $($process.StartTime)" -ForegroundColor Gray
        }
    }
}

# Stop processes by port first
Write-Host "`n🛑 Stopping processes by port..." -ForegroundColor Cyan
foreach ($port in $portsInUse) {
    Stop-ProcessByPort -Port $port
}

# Stop all Node.js processes
Write-Host "`n🛑 Stopping all Node.js processes..." -ForegroundColor Cyan
Stop-AllNodeProcesses

# Final verification
Write-Host "`n📊 Final Status Check..." -ForegroundColor Cyan
Start-Sleep -Seconds 2

$finalNodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
$finalPort3000 = Test-PortInUse -Port $WEB_INTERFACE_PORT
$finalPort3001 = Test-PortInUse -Port $DASHBOARD_PORT

Write-Host "`n📈 Final Results:" -ForegroundColor Cyan
if ($finalNodeProcesses) {
    Write-Host "❌ $($finalNodeProcesses.Count) Node.js processes still running" -ForegroundColor Red
} else {
    Write-Host "✅ All Node.js processes stopped" -ForegroundColor Green
}

if ($finalPort3000) {
    Write-Host "❌ Port $WEB_INTERFACE_PORT still in use" -ForegroundColor Red
} else {
    Write-Host "✅ Port $WEB_INTERFACE_PORT is free" -ForegroundColor Green
}

if ($finalPort3001) {
    Write-Host "❌ Port $DASHBOARD_PORT still in use" -ForegroundColor Red
} else {
    Write-Host "✅ Port $DASHBOARD_PORT is free" -ForegroundColor Green
}

Write-Host "`n🎉 MCP Server Toolkit Stop Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

if (-not $finalNodeProcesses -and -not $finalPort3000 -and -not $finalPort3001) {
    Write-Host "✅ All MCP servers and web interfaces have been stopped successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  Some processes or ports may still be active" -ForegroundColor Yellow
    if ($Force) {
        Write-Host "💡 Try running with -Force parameter if processes persist" -ForegroundColor Gray
    }
}

Write-Host "`n🔄 To restart, run: .\start-mcp-servers.ps1" -ForegroundColor Yellow 