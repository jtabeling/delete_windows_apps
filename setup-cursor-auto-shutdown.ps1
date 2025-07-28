# Setup Cursor Auto-Shutdown for MCP Servers
# Creates Windows Task Scheduler tasks to automatically monitor and shutdown MCP servers
param(
    [switch]$Install = $true,
    [switch]$Uninstall = $false,
    [switch]$Verbose = $false
)

# Configuration
$TASK_NAME = "MCP-AutoShutdown-Monitor"
$TASK_DESCRIPTION = "Monitors Cursor process and automatically stops MCP servers when Cursor exits"
$SCRIPT_PATH = Join-Path $PSScriptRoot "auto-shutdown-mcp.ps1"
$TRIGGER_NAME = "Cursor-Startup-Trigger"

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    $color = switch ($Type) {
        "Success" { "Green" }
        "Error" { "Red" }
        "Warning" { "Yellow" }
        default { "White" }
    }
    Write-Host "[$Type] $Message" -ForegroundColor $color
}

function Install-AutoShutdownTask {
    Write-Status "Installing MCP auto-shutdown monitor..." "Info"
    
    # Check if script exists
    if (-not (Test-Path $SCRIPT_PATH)) {
        Write-Status "Error: auto-shutdown-mcp.ps1 not found at $SCRIPT_PATH" "Error"
        return $false
    }
    
    # Create the task action
    $action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-ExecutionPolicy Bypass -File `"$SCRIPT_PATH`" -Background"
    
    # Create trigger that starts when Cursor starts
    $trigger = New-ScheduledTaskTrigger -AtStartup
    
    # Create settings
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RunOnlyIfNetworkAvailable
    
    # Create the task
    try {
        $task = Register-ScheduledTask -TaskName $TASK_NAME -Action $action -Trigger $trigger -Settings $settings -Description $TASK_DESCRIPTION -Force
        Write-Status "Task '$TASK_NAME' created successfully" "Success"
        
        # Enable the task
        Enable-ScheduledTask -TaskName $TASK_NAME
        Write-Status "Task '$TASK_NAME' enabled" "Success"
        
        return $true
    }
    catch {
        Write-Status "Error creating task: $($_.Exception.Message)" "Error"
        return $false
    }
}

function Uninstall-AutoShutdownTask {
    Write-Status "Uninstalling MCP auto-shutdown monitor..." "Info"
    
    try {
        # Stop the task if running
        Stop-ScheduledTask -TaskName $TASK_NAME -ErrorAction SilentlyContinue
        
        # Unregister the task
        Unregister-ScheduledTask -TaskName $TASK_NAME -Confirm:$false -ErrorAction SilentlyContinue
        
        Write-Status "Task '$TASK_NAME' uninstalled successfully" "Success"
        return $true
    }
    catch {
        Write-Status "Error uninstalling task: $($_.Exception.Message)" "Error"
        return $false
    }
}

function Test-TaskExists {
    try {
        $task = Get-ScheduledTask -TaskName $TASK_NAME -ErrorAction SilentlyContinue
        return $task -ne $null
    }
    catch {
        return $false
    }
}

function Show-Status {
    Write-Status "=== MCP Auto-Shutdown Monitor Status ===" "Info"
    Write-Status "Script Path: $SCRIPT_PATH" "Info"
    Write-Status "Task Name: $TASK_NAME" "Info"
    
    if (Test-Path $SCRIPT_PATH) {
        Write-Status "✓ Auto-shutdown script found" "Success"
    } else {
        Write-Status "✗ Auto-shutdown script not found" "Error"
    }
    
    if (Test-TaskExists) {
        Write-Status "✓ Scheduled task exists" "Success"
        $task = Get-ScheduledTask -TaskName $TASK_NAME
        Write-Status "Task State: $($task.State)" "Info"
        Write-Status "Task Enabled: $($task.Settings.Enabled)" "Info"
    } else {
        Write-Status "✗ Scheduled task not found" "Warning"
    }
    
    # Check if MCP servers are currently running
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
    
    Write-Status "MCP Servers Running: $(if ($mcpRunning) { 'Yes' } else { 'No' })" "Info"
}

# Main execution
if (-not (Test-Administrator)) {
    Write-Status "This script requires Administrator privileges" "Error"
    Write-Status "Please run PowerShell as Administrator" "Error"
    exit 1
}

if ($Uninstall) {
    Uninstall-AutoShutdownTask
} elseif ($Install) {
    Install-AutoShutdownTask
} else {
    Show-Status
}

Write-Status "Setup complete!" "Success"
Write-Status "The auto-shutdown monitor will now start automatically when you start Cursor" "Info" 