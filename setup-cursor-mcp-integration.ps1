# Cursor MCP Integration Setup Script
# This script sets up the MCP Server Toolkit integration with Cursor

Write-Host "=== Cursor MCP Integration Setup ===" -ForegroundColor Cyan
Write-Host "Setting up MCP Server Toolkit integration with Cursor..." -ForegroundColor Yellow

# Configuration
$mcpProjectPath = "H:\Cursor\mcp_server_toolkit"
$cursorConfigPath = "$env:APPDATA\Cursor\User\settings.json"
$backupPath = "$cursorConfigPath.backup"

# Function to check if MCP project exists
function Test-MCPProject {
    if (Test-Path $mcpProjectPath) {
        Write-Host "✅ MCP Server Toolkit found at: $mcpProjectPath" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ MCP Server Toolkit not found at: $mcpProjectPath" -ForegroundColor Red
        return $false
    }
}

# Function to backup Cursor settings
function Backup-CursorSettings {
    if (Test-Path $cursorConfigPath) {
        try {
            Copy-Item $cursorConfigPath $backupPath
            Write-Host "✅ Cursor settings backed up to: $backupPath" -ForegroundColor Green
            return $true
        } catch {
            Write-Host "⚠️  Could not backup Cursor settings: $($_.Exception.Message)" -ForegroundColor Yellow
            return $false
        }
    } else {
        Write-Host "ℹ️  No existing Cursor settings found, will create new" -ForegroundColor Blue
        return $true
    }
}

# Function to create MCP configuration
function Create-MCPConfiguration {
    $mcpConfig = @{
        "mcp" = @{
            "enabled" = $true
            "servers" = @{
                "filesystem" = @{
                    "command" = "node"
                    "args" = @("$mcpProjectPath\filesystem-mcp-server.js")
                    "env" = @{
                        "PROJECT_ROOT" = $mcpProjectPath
                        "ROOT_PATH" = "H:\Cursor"
                    }
                }
                "git" = @{
                    "command" = "node"
                    "args" = @("$mcpProjectPath\git-mcp-server.js")
                    "env" = @{
                        "PROJECT_ROOT" = $mcpProjectPath
                        "GIT_ROOT" = "H:\Cursor"
                    }
                }
                "docker" = @{
                    "command" = "node"
                    "args" = @("$mcpProjectPath\docker-mcp-server.js")
                    "env" = @{
                        "PROJECT_ROOT" = $mcpProjectPath
                    }
                }
                "system" = @{
                    "command" = "node"
                    "args" = @("$mcpProjectPath\system-mcp-server.js")
                    "env" = @{
                        "PROJECT_ROOT" = $mcpProjectPath
                    }
                }
                "working-docker" = @{
                    "command" = "node"
                    "args" = @("$mcpProjectPath\working-docker-mcp.js")
                    "env" = @{
                        "PROJECT_ROOT" = $mcpProjectPath
                    }
                }
            }
            "connection" = @{
                "protocol" = "stdio"
                "timeout" = 30000
                "retryAttempts" = 3
                "retryDelay" = 1000
            }
            "context" = @{
                "cacheEnabled" = $true
                "cacheTTL" = 300
                "maxContextSize" = 10000
                "includeProjectFiles" = $true
                "includeGitHistory" = $true
                "includeSystemInfo" = $true
            }
        }
    }
    
    return $mcpConfig
}

# Function to update Cursor settings
function Update-CursorSettings {
    param($mcpConfig)
    
    try {
        # Read existing settings or create new
        if (Test-Path $cursorConfigPath) {
            $existingSettings = Get-Content $cursorConfigPath -Raw | ConvertFrom-Json
        } else {
            $existingSettings = @{}
        }
        
        # Merge MCP configuration
        $existingSettings | Add-Member -MemberType NoteProperty -Name "mcp" -Value $mcpConfig.mcp -Force
        
        # Ensure directory exists
        $cursorConfigDir = Split-Path $cursorConfigPath -Parent
        if (!(Test-Path $cursorConfigDir)) {
            New-Item -ItemType Directory -Path $cursorConfigDir -Force | Out-Null
        }
        
        # Write updated settings
        $existingSettings | ConvertTo-Json -Depth 10 | Out-File $cursorConfigPath -Encoding UTF8
        
        Write-Host "✅ Cursor settings updated with MCP configuration" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "❌ Error updating Cursor settings: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to test MCP servers
function Test-MCPServers {
    Write-Host "`n🧪 Testing MCP servers..." -ForegroundColor Cyan
    
    $servers = @(
        @{Name="Filesystem"; File="filesystem-mcp-server.js"},
        @{Name="Git"; File="git-mcp-server.js"},
        @{Name="Docker"; File="docker-mcp-server.js"},
        @{Name="System"; File="system-mcp-server.js"}
    )
    
    $results = @()
    
    foreach ($server in $servers) {
        $serverPath = Join-Path $mcpProjectPath $server.File
        
        if (Test-Path $serverPath) {
            Write-Host "✅ $($server.Name) server found: $serverPath" -ForegroundColor Green
            $results += @{Name=$server.Name; Status="Found"; Path=$serverPath}
        } else {
            Write-Host "❌ $($server.Name) server not found: $serverPath" -ForegroundColor Red
            $results += @{Name=$server.Name; Status="Missing"; Path=$serverPath}
        }
    }
    
    return $results
}

# Function to create startup script
function Create-StartupScript {
    $startupScript = @"
# MCP Server Startup Script
# Run this script to start all MCP servers

Write-Host "Starting MCP servers..." -ForegroundColor Cyan

cd "$mcpProjectPath"

# Start servers in background
Start-Process -FilePath "node" -ArgumentList "filesystem-mcp-server.js" -WindowStyle Hidden
Start-Process -FilePath "node" -ArgumentList "git-mcp-server.js" -WindowStyle Hidden
Start-Process -FilePath "node" -ArgumentList "docker-mcp-server.js" -WindowStyle Hidden
Start-Process -FilePath "node" -ArgumentList "system-mcp-server.js" -WindowStyle Hidden

Write-Host "MCP servers started in background" -ForegroundColor Green
Write-Host "You can now use Cursor with enhanced MCP capabilities" -ForegroundColor Cyan
"@
    
    $startupPath = Join-Path $mcpProjectPath "start-mcp-servers.ps1"
    $startupScript | Out-File $startupPath -Encoding UTF8
    
    Write-Host "✅ Startup script created: $startupPath" -ForegroundColor Green
    return $startupPath
}

# Function to create documentation
function Create-Documentation {
    $docs = @"
# Cursor MCP Integration Setup Complete

## What was configured:
- MCP Server Toolkit integration with Cursor
- 5 MCP servers configured (filesystem, git, docker, system, working-docker)
- Automatic startup configuration
- Context caching and connection settings

## Available MCP Servers:
1. **Filesystem Server**: Access to project files and structure
2. **Git Server**: Repository history and commit information  
3. **Docker Server**: Container management and information
4. **System Server**: System resources and information
5. **Working Docker Server**: Production-ready Docker integration

## How to use:
1. Restart Cursor to load the new configuration
2. MCP servers will start automatically
3. Cursor AI will now have access to enhanced context

## Manual server management:
- Start servers: `H:\Cursor\mcp_server_toolkit\start-mcp-servers.ps1`
- Stop servers: Use Task Manager to end Node.js processes
- Check status: Look for Node.js processes in Task Manager

## Troubleshooting:
- If servers don't start, check Node.js installation
- Verify paths in Cursor settings
- Check logs in Cursor output panel

## Configuration files:
- Cursor settings: $cursorConfigPath
- Backup: $backupPath
- MCP project: $mcpProjectPath

Setup completed on: $(Get-Date)
"@
    
    $docsPath = Join-Path $mcpProjectPath "CURSOR_INTEGRATION_SETUP.md"
    $docs | Out-File $docsPath -Encoding UTF8
    
    Write-Host "✅ Documentation created: $docsPath" -ForegroundColor Green
    return $docsPath
}

# Main execution
try {
    Write-Host "`n🔍 Step 1: Checking MCP project..." -ForegroundColor Cyan
    if (!(Test-MCPProject)) {
        Write-Host "❌ Setup failed: MCP project not found" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n💾 Step 2: Backing up Cursor settings..." -ForegroundColor Cyan
    Backup-CursorSettings | Out-Null
    
    Write-Host "`n⚙️  Step 3: Creating MCP configuration..." -ForegroundColor Cyan
    $mcpConfig = Create-MCPConfiguration
    
    Write-Host "`n📝 Step 4: Updating Cursor settings..." -ForegroundColor Cyan
    if (!(Update-CursorSettings -mcpConfig $mcpConfig)) {
        Write-Host "❌ Setup failed: Could not update Cursor settings" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n🧪 Step 5: Testing MCP servers..." -ForegroundColor Cyan
    $serverResults = Test-MCPServers
    
    Write-Host "`n🚀 Step 6: Creating startup script..." -ForegroundColor Cyan
    $startupPath = Create-StartupScript
    
    Write-Host "`n📚 Step 7: Creating documentation..." -ForegroundColor Cyan
    $docsPath = Create-Documentation
    
    # Summary
    Write-Host "`n" + "="*60 -ForegroundColor Gray
    Write-Host "SETUP COMPLETE!" -ForegroundColor Green
    Write-Host "="*60 -ForegroundColor Gray
    
    Write-Host "`n✅ MCP Server Toolkit integration configured successfully!" -ForegroundColor Green
    Write-Host "📁 MCP Project: $mcpProjectPath" -ForegroundColor White
    Write-Host "⚙️  Cursor Settings: $cursorConfigPath" -ForegroundColor White
    Write-Host "🚀 Startup Script: $startupPath" -ForegroundColor White
    Write-Host "📚 Documentation: $docsPath" -ForegroundColor White
    
    Write-Host "`n📊 Server Status:" -ForegroundColor Cyan
    foreach ($result in $serverResults) {
        $status = if ($result.Status -eq "Found") { "✅" } else { "❌" }
        Write-Host "   $status $($result.Name): $($result.Status)" -ForegroundColor White
    }
    
    Write-Host "`n🎯 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Restart Cursor to load the new configuration" -ForegroundColor White
    Write-Host "2. MCP servers will start automatically" -ForegroundColor White
    Write-Host "3. Test the integration by asking Cursor about your projects" -ForegroundColor White
    Write-Host "4. Check the documentation for troubleshooting" -ForegroundColor White
    
    Write-Host "`n🎉 Integration ready! Your Cursor AI now has enhanced capabilities!" -ForegroundColor Green
    
} catch {
    Write-Host "`n❌ Setup failed with error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Gray
}

Write-Host "`nPress any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 