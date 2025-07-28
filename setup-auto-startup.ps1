# MCP Server Toolkit Auto-Startup Setup
# Creates Windows Task Scheduler tasks for automatic startup

param(
    [switch]$Install = $true,
    [switch]$Uninstall = $false,
    [switch]$Verbose = $false
)

Write-Host "🔧 MCP Server Toolkit Auto-Startup Setup" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

# Configuration
$TASK_NAME = "MCP-Server-Toolkit-Startup"
$TASK_DESCRIPTION = "Automatically starts MCP Server Toolkit on system startup"
$SCRIPT_PATH = Join-Path $PSScriptRoot "start-mcp-servers.ps1"
$WORKING_DIRECTORY = $PSScriptRoot

# Function to check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Function to create the scheduled task
function New-MCPScheduledTask {
    try {
        Write-Host "📋 Creating scheduled task: $TASK_NAME" -ForegroundColor Yellow
        
        # Define the action
        $action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-ExecutionPolicy Bypass -File `"$SCRIPT_PATH`"" -WorkingDirectory $WORKING_DIRECTORY
        
        # Define the trigger (at startup)
        $trigger = New-ScheduledTaskTrigger -AtStartup
        
        # Define the settings
        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RunOnlyIfNetworkAvailable
        
        # Define the principal (run as current user with highest privileges)
        $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Highest
        
        # Create the task
        $task = New-ScheduledTask -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description $TASK_DESCRIPTION
        
        # Register the task
        Register-ScheduledTask -TaskName $TASK_NAME -InputObject $task -Force
        
        Write-Host "✅ Scheduled task created successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ Error creating scheduled task: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to remove the scheduled task
function Remove-MCPScheduledTask {
    try {
        Write-Host "🗑️  Removing scheduled task: $TASK_NAME" -ForegroundColor Yellow
        
        # Check if task exists
        $existingTask = Get-ScheduledTask -TaskName $TASK_NAME -ErrorAction SilentlyContinue
        if ($existingTask) {
            Unregister-ScheduledTask -TaskName $TASK_NAME -Confirm:$false
            Write-Host "✅ Scheduled task removed successfully" -ForegroundColor Green
            return $true
        } else {
            Write-Host "ℹ️  Scheduled task not found" -ForegroundColor Gray
            return $true
        }
    }
    catch {
        Write-Host "❌ Error removing scheduled task: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to check if task exists
function Test-MCPScheduledTask {
    try {
        $task = Get-ScheduledTask -TaskName $TASK_NAME -ErrorAction SilentlyContinue
        return $task -ne $null
    }
    catch {
        return $false
    }
}

# Function to get task status
function Get-MCPScheduledTaskStatus {
    try {
        $task = Get-ScheduledTask -TaskName $TASK_NAME -ErrorAction SilentlyContinue
        if ($task) {
            return @{
                Exists = $true
                State = $task.State
                LastRunTime = $task.LastRunTime
                NextRunTime = $task.NextRunTime
                Enabled = $task.Settings.Enabled
            }
        } else {
            return @{
                Exists = $false
                State = $null
                LastRunTime = $null
                NextRunTime = $null
                Enabled = $false
            }
        }
    }
    catch {
        return @{
            Exists = $false
            State = $null
            LastRunTime = $null
            NextRunTime = $null
            Enabled = $false
        }
    }
}

# Main execution
Write-Host "🔍 Checking prerequisites..." -ForegroundColor Yellow

# Check if running as administrator
if (-not (Test-Administrator)) {
    Write-Host "❌ This script requires administrator privileges" -ForegroundColor Red
    Write-Host "💡 Please run PowerShell as Administrator and try again" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Running as administrator" -ForegroundColor Green

# Check if startup script exists
if (-not (Test-Path $SCRIPT_PATH)) {
    Write-Host "❌ Startup script not found: $SCRIPT_PATH" -ForegroundColor Red
    Write-Host "💡 Please ensure start-mcp-servers.ps1 exists in the current directory" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Startup script found" -ForegroundColor Green

# Check current task status
$currentStatus = Get-MCPScheduledTaskStatus
if ($currentStatus.Exists) {
    Write-Host "📋 Current task status:" -ForegroundColor Cyan
    Write-Host "   State: $($currentStatus.State)" -ForegroundColor Gray
    Write-Host "   Enabled: $($currentStatus.Enabled)" -ForegroundColor Gray
    if ($currentStatus.LastRunTime) {
        Write-Host "   Last Run: $($currentStatus.LastRunTime)" -ForegroundColor Gray
    }
    if ($currentStatus.NextRunTime) {
        Write-Host "   Next Run: $($currentStatus.NextRunTime)" -ForegroundColor Gray
    }
}

# Perform installation or uninstallation
if ($Uninstall) {
    Write-Host "`n🛑 Uninstalling auto-startup..." -ForegroundColor Red
    if (Remove-MCPScheduledTask) {
        Write-Host "`n🎉 Auto-startup uninstalled successfully!" -ForegroundColor Green
        Write-Host "================================================" -ForegroundColor Cyan
        Write-Host "📋 MCP servers will no longer start automatically" -ForegroundColor Yellow
        Write-Host "🔄 To start manually, run: .\start-mcp-servers.ps1" -ForegroundColor Yellow
    } else {
        Write-Host "`n❌ Failed to uninstall auto-startup" -ForegroundColor Red
        exit 1
    }
} elseif ($Install) {
    Write-Host "`n🚀 Installing auto-startup..." -ForegroundColor Green
    
    # Remove existing task if it exists
    if ($currentStatus.Exists) {
        Write-Host "🔄 Removing existing task..." -ForegroundColor Yellow
        Remove-MCPScheduledTask | Out-Null
    }
    
    # Create new task
    if (New-MCPScheduledTask) {
        Write-Host "`n🎉 Auto-startup installed successfully!" -ForegroundColor Green
        Write-Host "================================================" -ForegroundColor Cyan
        Write-Host "✅ MCP servers will now start automatically on system startup" -ForegroundColor Green
        Write-Host "🌐 Web Interface: http://localhost:3000" -ForegroundColor Yellow
        Write-Host "📊 Dashboard: http://localhost:3001" -ForegroundColor Yellow
        Write-Host "`n📋 Task Details:" -ForegroundColor Cyan
        Write-Host "   Name: $TASK_NAME" -ForegroundColor Gray
        Write-Host "   Trigger: At system startup" -ForegroundColor Gray
        Write-Host "   Script: $SCRIPT_PATH" -ForegroundColor Gray
        Write-Host "   Privileges: Highest" -ForegroundColor Gray
        
        Write-Host "`n💡 To test immediately, run: .\start-mcp-servers.ps1" -ForegroundColor Yellow
        Write-Host "🛑 To uninstall, run: .\setup-auto-startup.ps1 -Uninstall" -ForegroundColor Yellow
    } else {
        Write-Host "`n❌ Failed to install auto-startup" -ForegroundColor Red
        exit 1
    }
}

# Display final status
Write-Host "`n📊 Final Status:" -ForegroundColor Cyan
$finalStatus = Get-MCPScheduledTaskStatus
if ($finalStatus.Exists) {
    Write-Host "✅ Auto-startup is configured and enabled" -ForegroundColor Green
    Write-Host "   Task State: $($finalStatus.State)" -ForegroundColor Gray
} else {
    Write-Host "❌ Auto-startup is not configured" -ForegroundColor Red
}

Write-Host "`n🔧 Setup complete!" -ForegroundColor Green 