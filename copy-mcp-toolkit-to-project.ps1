# Copy MCP Toolkit to New Project
# Automatically copies all essential MCP server toolkit files to a target project
param(
    [Parameter(Mandatory=$true)]
    [string]$TargetProjectPath,
    
    [Parameter(Mandatory=$false)]
    [string]$MCPToolkitPath = "H:\Cursor\mcp_server_toolkit",
    
    [switch]$Minimal = $false,
    [switch]$DetailedLogging = $false,
    [switch]$SkipVerification = $false
)

# Configuration
$SCRIPT_NAME = "Copy MCP Toolkit to Project"
$SOURCE_DIR = $PWD
$LOG_FILE = "mcp-toolkit-copy.log"

function Write-CopyLog {
    param([string]$Message, [string]$Type = "Info")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Type] $Message"
    
    $color = switch ($Type) {
        "Success" { "Green" }
        "Error" { "Red" }
        "Warning" { "Yellow" }
        "Copy" { "Cyan" }
        default { "White" }
    }
    
    Write-Host $logMessage -ForegroundColor $color
    Add-Content -Path $LOG_FILE -Value $logMessage
}

function Test-Prerequisites {
    Write-CopyLog "Checking prerequisites..." "Info"
    
    # Check if target directory exists
    if (-not (Test-Path $TargetProjectPath)) {
        Write-CopyLog "Creating target directory: $TargetProjectPath" "Info"
        try {
            New-Item -ItemType Directory -Path $TargetProjectPath -Force | Out-Null
            Write-CopyLog "Target directory created successfully" "Success"
        }
        catch {
            Write-CopyLog "Error creating target directory: $($_.Exception.Message)" "Error"
            return $false
        }
    }
    
    # Check if MCP toolkit path exists
    if (-not (Test-Path $MCPToolkitPath)) {
        Write-CopyLog "Warning: MCP toolkit path not found: $MCPToolkitPath" "Warning"
        Write-CopyLog "You may need to update paths in configuration files after copying" "Warning"
    }
    
    # Check Node.js
    try {
        $nodeVersion = node --version 2>$null
        if ($nodeVersion) {
            Write-CopyLog "Node.js found: $nodeVersion" "Success"
        } else {
            Write-CopyLog "Warning: Node.js not found. MCP servers may not work." "Warning"
        }
    }
    catch {
        Write-CopyLog "Warning: Node.js not found. MCP servers may not work." "Warning"
    }
    
    # Check PowerShell version
    $psVersion = $PSVersionTable.PSVersion
    Write-CopyLog "PowerShell version: $psVersion" "Info"
    
    return $true
}

function Get-EssentialFiles {
    if ($Minimal) {
        Write-CopyLog "Using minimal file set (core MCP functionality only)" "Info"
        return @(
            "start-mcp-servers.ps1",
            "stop-mcp-servers.ps1", 
            "check-mcp-status.ps1",
            "mcp-project-config.json",
            "mcp-dashboard-server.js"
        )
    } else {
        Write-CopyLog "Using complete file set (full MCP toolkit with startup and auto-shutdown)" "Info"
        return @(
            # Core MCP Management
            "start-mcp-servers.ps1",
            "stop-mcp-servers.ps1", 
            "check-mcp-status.ps1",
            "mcp-project-config.json",
            
            # Project Startup System
            "start-project-with-mcp.ps1",
            "start-project.bat",
            "cursor-project-startup.ps1",
            
            # Auto-Shutdown System
            "auto-shutdown-mcp.ps1",
            "setup-cursor-auto-shutdown.ps1",
            "start-auto-shutdown-monitor.ps1",
            
            # Web Interface
            "mcp-dashboard-server.js",
            
            # Configuration Files
            "cursor-mcp-integration-config.json",
            "cursor-mcp-extension-config.json",
            
            # Documentation
            "MCP_TOOLKIT_README.md",
            "CURSOR_MCP_INTEGRATION_GUIDE.md"
        )
    }
}

function Copy-MCPFiles {
    param([string[]]$Files)
    
    Write-CopyLog "Starting file copy process..." "Info"
    $successCount = 0
    $errorCount = 0
    
    foreach ($file in $Files) {
        $sourceFile = Join-Path $SOURCE_DIR $file
        $targetFile = Join-Path $TargetProjectPath $file
        
        if (Test-Path $sourceFile) {
            try {
                Copy-Item $sourceFile $targetFile -Force
                Write-CopyLog "Copied: $file" "Copy"
                $successCount++
            }
            catch {
                Write-CopyLog "Error copying $file : $($_.Exception.Message)" "Error"
                $errorCount++
            }
        } else {
            Write-CopyLog "Warning: Source file not found: $file" "Warning"
            $errorCount++
        }
    }
    
    Write-CopyLog "Copy process complete. Success: $successCount, Errors: $errorCount" "Info"
    return $errorCount -eq 0
}

function Update-ConfigurationPaths {
    Write-CopyLog "Updating configuration paths..." "Info"
    
    $configFile = Join-Path $TargetProjectPath "mcp-project-config.json"
    if (Test-Path $configFile) {
        try {
            $config = Get-Content $configFile | ConvertFrom-Json
            
            # Update MCP toolkit path if different
            if ($config.paths.mcpToolkitPath -ne $MCPToolkitPath) {
                $config.paths.mcpToolkitPath = $MCPToolkitPath
                $config | ConvertTo-Json -Depth 10 | Set-Content $configFile
                Write-CopyLog "Updated MCP toolkit path in configuration" "Success"
            }
        }
        catch {
            Write-CopyLog "Warning: Could not update configuration paths: $($_.Exception.Message)" "Warning"
        }
    }
}

function Create-ProjectStartupScript {
    if (-not $Minimal) {
        Write-CopyLog "Creating project-specific startup script..." "Info"
        
        $startupScript = @"
# Project-Specific MCP Startup
# Auto-generated by copy-mcp-toolkit-to-project.ps1

Write-Host "Starting MCP Toolkit for: $(Split-Path $PWD -Leaf)" -ForegroundColor Green
Write-Host "Project directory: $PWD" -ForegroundColor Cyan

# Start MCP servers
if (Test-Path "start-project-with-mcp.ps1") {
    .\start-project-with-mcp.ps1
} else {
    Write-Host "Error: MCP startup script not found!" -ForegroundColor Red
}
"@
        
        $startupFile = Join-Path $TargetProjectPath "start-mcp-toolkit.bat"
        $startupScript | Out-File -FilePath $startupFile -Encoding UTF8
        Write-CopyLog "Created project startup script: start-mcp-toolkit.bat" "Success"
    }
}

function Test-MCPInstallation {
    if ($SkipVerification) {
        Write-CopyLog "Skipping verification as requested" "Info"
        return $true
    }
    
    Write-CopyLog "Testing MCP installation..." "Info"
    
    # Change to target directory
    $originalDir = $PWD
    Set-Location $TargetProjectPath
    
    try {
        # Test if startup script exists
        if (Test-Path "start-project-with-mcp.ps1") {
            Write-CopyLog "Testing startup script..." "Info"
            
            # Test with SkipMCP flag to avoid actually starting servers
            $result = & ".\start-project-with-mcp.ps1" -SkipMCP 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-CopyLog "Startup script test successful" "Success"
            } else {
                Write-CopyLog "Warning: Startup script test failed" "Warning"
            }
        }
        
        # Test status script
        if (Test-Path "check-mcp-status.ps1") {
            Write-CopyLog "Testing status script..." "Info"
            $result = & ".\check-mcp-status.ps1" 2>&1
            Write-CopyLog "Status script test completed" "Info"
        }
        
        Write-CopyLog "MCP installation verification complete" "Success"
        return $true
    }
    catch {
        Write-CopyLog "Error during verification: $($_.Exception.Message)" "Error"
        return $false
    }
    finally {
        Set-Location $originalDir
    }
}

function Show-UsageInstructions {
    Write-CopyLog "=== MCP Toolkit Installation Complete ===" "Info"
    Write-CopyLog "Target project: $TargetProjectPath" "Info"
    
    if ($Minimal) {
        Write-CopyLog "Installation type: Minimal (core MCP functionality)" "Info"
        Write-CopyLog "Available commands:" "Info"
        Write-CopyLog "  .\start-mcp-servers.ps1    - Start MCP servers" "Copy"
        Write-CopyLog "  .\stop-mcp-servers.ps1     - Stop MCP servers" "Copy"
        Write-CopyLog "  .\check-mcp-status.ps1     - Check server status" "Copy"
    } else {
        Write-CopyLog "Installation type: Complete (full MCP toolkit with startup and auto-shutdown)" "Info"
        Write-CopyLog "Available commands:" "Info"
        Write-CopyLog "  .\start-project.bat        - One-click project startup" "Copy"
        Write-CopyLog "  .\start-project-with-mcp.ps1 - Full project startup with logging" "Copy"
        Write-CopyLog "  .\cursor-project-startup.ps1 - Cursor-specific startup" "Copy"
        Write-CopyLog "  .\start-mcp-servers.ps1    - Start MCP servers only" "Copy"
        Write-CopyLog "  .\stop-mcp-servers.ps1     - Stop MCP servers" "Copy"
        Write-CopyLog "  .\check-mcp-status.ps1     - Check server status" "Copy"
    }
    
    Write-CopyLog "Web interfaces:" "Info"
    Write-CopyLog "  http://localhost:3000 - MCP Web Interface" "Copy"
    Write-CopyLog "  http://localhost:3001 - MCP Dashboard" "Copy"
    
    Write-CopyLog "Documentation:" "Info"
    Write-CopyLog "  MCP_TOOLKIT_README.md - Complete usage guide" "Copy"
    Write-CopyLog "  CURSOR_MCP_INTEGRATION_GUIDE.md - Integration guide" "Copy"
    
    Write-CopyLog "Log file: $LOG_FILE" "Info"
}

# Main execution
Write-CopyLog "=== $SCRIPT_NAME ===" "Info"
Write-CopyLog "Source directory: $SOURCE_DIR" "Info"
Write-CopyLog "Target directory: $TargetProjectPath" "Info"
Write-CopyLog "MCP toolkit path: $MCPToolkitPath" "Info"

# Check prerequisites
if (-not (Test-Prerequisites)) {
    Write-CopyLog "Prerequisites check failed. Exiting." "Error"
    exit 1
}

# Get file list
$filesToCopy = Get-EssentialFiles

# Copy files
if (-not (Copy-MCPFiles -Files $filesToCopy)) {
    Write-CopyLog "File copy process had errors. Continuing with setup..." "Warning"
}

# Update configuration
Update-ConfigurationPaths

# Create project-specific startup script
Create-ProjectStartupScript

# Test installation
if (Test-MCPInstallation) {
    Write-CopyLog "MCP toolkit installation successful!" "Success"
} else {
    Write-CopyLog "MCP toolkit installation completed with warnings" "Warning"
}

# Show usage instructions
Show-UsageInstructions

Write-CopyLog "=== Installation Complete ===" "Success" 