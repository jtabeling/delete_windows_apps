# MCP Server Toolkit Startup Script
# Automatically starts all MCP servers and web interfaces

param(
    [switch]$Background = $true,
    [switch]$Verbose = $false
)

Write-Host "🚀 Starting MCP Server Toolkit..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

# Configuration
$MCP_TOOLKIT_PATH = "H:\Cursor\mcp_server_toolkit"
$WEB_INTERFACE_PORT = 3000
$DASHBOARD_PORT = 3001

# MCP Servers to start
$MCP_SERVERS = @(
    @{ Name = "filesystem"; File = "filesystem-mcp-server.js"; Description = "File system operations" },
    @{ Name = "git"; File = "git-mcp-server.js"; Description = "Git repository management" },
    @{ Name = "docker"; File = "docker-mcp-server.js"; Description = "Docker container management" },
    @{ Name = "system"; File = "system-mcp-server.js"; Description = "System information and operations" },
    @{ Name = "memory"; File = "memory-mcp-server.js"; Description = "Memory and cache management" },
    @{ Name = "docker-v2"; File = "docker-mcp-server-v2.js"; Description = "Enhanced Docker operations" },
    @{ Name = "docker-cli"; File = "docker-cli-server.js"; Description = "Docker CLI interface" },
    @{ Name = "registry"; File = "mcp-server-registry.js"; Description = "MCP server registry" }
)

# Web Services to start
$WEB_SERVICES = @(
    @{ Name = "web-interface"; File = "mcp-web-interface.js"; Port = $WEB_INTERFACE_PORT; Description = "MCP Web Interface" },
    @{ Name = "dashboard"; File = "mcp-dashboard-server.js"; Port = $DASHBOARD_PORT; Description = "MCP Server Dashboard" }
)

# Function to check if a port is in use
function Test-PortInUse {
    param([int]$Port)
    $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    return $connection -ne $null
}

# Function to check if a process is running
function Test-ProcessRunning {
    param([string]$ProcessName)
    $process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    return $process -ne $null
}

# Function to start a server
function Start-MCPServer {
    param(
        [string]$Name,
        [string]$File,
        [string]$Description,
        [int]$Port = 0
    )
    
    $filePath = Join-Path $MCP_TOOLKIT_PATH $File
    
    if (-not (Test-Path $filePath)) {
        Write-Host "❌ Server file not found: $File" -ForegroundColor Red
        return $false
    }
    
    # Check if already running
    if ($Port -gt 0 -and (Test-PortInUse -Port $Port)) {
        Write-Host "⚠️  Port $Port already in use for $Name" -ForegroundColor Yellow
        return $true
    }
    
    try {
        if ($Background) {
            $process = Start-Process -FilePath "node" -ArgumentList $File -WorkingDirectory $MCP_TOOLKIT_PATH -WindowStyle Hidden -PassThru
        } else {
            $process = Start-Process -FilePath "node" -ArgumentList $File -WorkingDirectory $MCP_TOOLKIT_PATH -PassThru
        }
        
        if ($process) {
            Write-Host "✅ Started $Name ($Description)" -ForegroundColor Green
            if ($Verbose) {
                Write-Host "   Process ID: $($process.Id)" -ForegroundColor Gray
                if ($Port -gt 0) {
                    Write-Host "   Port: $Port" -ForegroundColor Gray
                }
            }
            return $true
        } else {
            Write-Host "❌ Failed to start $Name" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "❌ Error starting $Name`: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to wait for port to be available
function Wait-ForPort {
    param([int]$Port, [int]$TimeoutSeconds = 30)
    
    $startTime = Get-Date
    $endTime = $startTime.AddSeconds($TimeoutSeconds)
    
    while ((Get-Date) -lt $endTime) {
        if (Test-PortInUse -Port $Port) {
            return $true
        }
        Start-Sleep -Milliseconds 500
    }
    
    return $false
}

# Main execution
Write-Host "📁 MCP Toolkit Path: $MCP_TOOLKIT_PATH" -ForegroundColor Cyan
Write-Host "🔍 Checking MCP toolkit directory..." -ForegroundColor Yellow

if (-not (Test-Path $MCP_TOOLKIT_PATH)) {
    Write-Host "❌ MCP Toolkit directory not found: $MCP_TOOLKIT_PATH" -ForegroundColor Red
    exit 1
}

Write-Host "✅ MCP Toolkit directory found" -ForegroundColor Green

# Stop existing Node.js processes if requested
$existingProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
if ($existingProcesses) {
    Write-Host "🔄 Found $($existingProcesses.Count) existing Node.js processes" -ForegroundColor Yellow
    if ($Background) {
        Write-Host "   Running in background mode - existing processes will continue" -ForegroundColor Gray
    }
}

# Start MCP Servers
Write-Host "`n🚀 Starting MCP Servers..." -ForegroundColor Cyan
$startedServers = 0

foreach ($server in $MCP_SERVERS) {
    if (Start-MCPServer -Name $server.Name -File $server.File -Description $server.Description) {
        $startedServers++
    }
    Start-Sleep -Milliseconds 500  # Small delay between starts
}

Write-Host "✅ Started $startedServers out of $($MCP_SERVERS.Count) MCP servers" -ForegroundColor Green

# Start Web Services
Write-Host "`n🌐 Starting Web Services..." -ForegroundColor Cyan
$startedWebServices = 0

foreach ($service in $WEB_SERVICES) {
    if (Start-MCPServer -Name $service.Name -File $service.File -Description $service.Description -Port $service.Port) {
        $startedWebServices++
        
        # Wait for port to be available
        if (Wait-ForPort -Port $service.Port) {
            Write-Host "   ✅ Port $($service.Port) is now active" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Port $($service.Port) may not be ready yet" -ForegroundColor Yellow
        }
    }
    Start-Sleep -Seconds 2  # Longer delay for web services
}

Write-Host "✅ Started $startedWebServices out of $($WEB_SERVICES.Count) web services" -ForegroundColor Green

# Final status check
Write-Host "`n📊 Final Status Check..." -ForegroundColor Cyan
Start-Sleep -Seconds 3

$finalNodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
Write-Host "📈 Total Node.js processes running: $($finalNodeProcesses.Count)" -ForegroundColor Green

# Check ports
foreach ($service in $WEB_SERVICES) {
    if (Test-PortInUse -Port $service.Port) {
        Write-Host "✅ Port $($service.Port) ($($service.Name)) is active" -ForegroundColor Green
    } else {
        Write-Host "❌ Port $($service.Port) ($($service.Name)) is not active" -ForegroundColor Red
    }
}

# Display access information
Write-Host "`n🎉 MCP Server Toolkit Startup Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "🌐 Web Interface: http://localhost:$WEB_INTERFACE_PORT" -ForegroundColor Yellow
Write-Host "📊 Dashboard: http://localhost:$DASHBOARD_PORT" -ForegroundColor Yellow
Write-Host "`n📋 Available MCP Servers:" -ForegroundColor Cyan
foreach ($server in $MCP_SERVERS) {
    Write-Host "   • $($server.Name) - $($server.Description)" -ForegroundColor Gray
}

Write-Host "`n💡 Tips:" -ForegroundColor Cyan
Write-Host "   • Use the Dashboard to manage servers" -ForegroundColor Gray
Write-Host "   • Check server status in real-time" -ForegroundColor Gray
Write-Host "   • All MCP toolkit content is now available" -ForegroundColor Gray

if ($Background) {
    Write-Host "`n🔄 To stop all servers, run: Get-Process node | Stop-Process" -ForegroundColor Yellow
} 