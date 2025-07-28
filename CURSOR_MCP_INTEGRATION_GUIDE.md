# Cursor MCP Integration Guide

## 🎯 Overview

This guide helps you integrate your MCP Server Toolkit with Cursor to enhance AI capabilities with external data sources and tools.

## 📁 Files Created

1. **`cursor-mcp-integration-config.json`** - Main integration configuration
2. **`cursor-mcp-extension-config.json`** - Cursor extension configuration
3. **`setup-cursor-mcp-integration.ps1`** - Automated setup script
4. **`CURSOR_MCP_INTEGRATION_GUIDE.md`** - This guide

## 🚀 Quick Setup

### Option 1: Automated Setup (Recommended)
```powershell
# Run the automated setup script
.\setup-cursor-mcp-integration.ps1
```

### Option 2: Manual Setup
1. Copy the configuration from `cursor-mcp-integration-config.json`
2. Add it to your Cursor settings
3. Restart Cursor

## ⚙️ Configuration Details

### MCP Servers Configured

| Server | Type | Description | Priority |
|--------|------|-------------|----------|
| **filesystem** | File System | Project files and structure | High |
| **git** | Git History | Repository information | High |
| **docker** | Docker | Container management | Medium |
| **system** | System | System resources | Medium |
| **working-docker** | Docker | Production Docker integration | High |

### Connection Settings
- **Protocol**: stdio
- **Timeout**: 30 seconds
- **Retry Attempts**: 3
- **Retry Delay**: 1 second

### Context Settings
- **Cache Enabled**: Yes
- **Cache TTL**: 5 minutes
- **Max Context Size**: 10KB
- **Include Project Files**: Yes
- **Include Git History**: Yes
- **Include System Info**: Yes

## 🎮 How to Use

### 1. Start the Integration
```powershell
# Navigate to your MCP project
cd H:\Cursor\mcp_server_toolkit

# Start servers manually (if auto-start is disabled)
.\start-mcp-servers.ps1
```

### 2. Test the Integration
Once Cursor is restarted with the new configuration:

1. **Ask about project structure**:
   ```
   "Show me the file structure of my current project"
   ```

2. **Get Git history**:
   ```
   "What are the recent commits in this repository?"
   ```

3. **Docker information**:
   ```
   "What Docker containers are running?"
   ```

4. **System resources**:
   ```
   "What's the current system resource usage?"
   ```

### 3. Enhanced AI Capabilities

With MCP integration, Cursor can now:
- **Access project files** and understand your codebase structure
- **Read Git history** and understand code evolution
- **Monitor Docker containers** and provide container insights
- **Check system resources** and provide performance recommendations
- **Provide context-aware suggestions** based on your actual project

## 🔧 Server Management

### Start Servers
```powershell
# Automated startup
.\start-mcp-servers.ps1

# Manual startup (individual servers)
node filesystem-mcp-server.js
node git-mcp-server.js
node docker-mcp-server.js
node system-mcp-server.js
```

### Stop Servers
```powershell
# Find and stop Node.js processes
Get-Process node | Stop-Process

# Or use Task Manager to end Node.js processes
```

### Check Server Status
```powershell
# Check if servers are running
Get-Process node | Where-Object { $_.ProcessName -eq "node" }
```

## 🛠️ Troubleshooting

### Common Issues

#### 1. Servers Not Starting
**Problem**: MCP servers fail to start
**Solution**:
```powershell
# Check Node.js installation
node --version

# Check if files exist
Test-Path "H:\Cursor\mcp_server_toolkit\filesystem-mcp-server.js"
```

#### 2. Cursor Not Recognizing MCP
**Problem**: Cursor doesn't use MCP context
**Solution**:
- Restart Cursor completely
- Check Cursor settings for MCP configuration
- Verify server processes are running

#### 3. Permission Issues
**Problem**: Access denied errors
**Solution**:
```powershell
# Run as Administrator
Start-Process PowerShell -Verb RunAs
```

#### 4. Path Issues
**Problem**: Servers can't find files
**Solution**:
- Verify all paths in configuration
- Use absolute paths instead of relative
- Check file permissions

### Debug Mode

Enable debug logging:
```json
{
  "mcp": {
    "logging": {
      "level": "debug",
      "file": "H:\\Cursor\\mcp_server_toolkit\\debug.log"
    }
  }
}
```

## 📊 Performance Optimization

### Recommended Settings
```json
{
  "mcp": {
    "context": {
      "cacheEnabled": true,
      "cacheTTL": 300,
      "maxContextSize": 10000
    },
    "connection": {
      "timeout": 30000,
      "retryAttempts": 3
    }
  }
}
```

### Memory Management
- Monitor Node.js process memory usage
- Restart servers periodically if needed
- Use process monitoring tools

## 🔒 Security Considerations

### Current Security Settings
- **Allowed Paths**: H:\Cursor and subdirectories
- **Authentication**: Disabled (for local development)
- **Command Restrictions**: None (for full functionality)

### Production Recommendations
```json
{
  "mcp": {
    "security": {
      "requireAuthentication": true,
      "allowedPaths": ["specific/project/paths"],
      "restrictedCommands": ["dangerous/commands"]
    }
  }
}
```

## 📈 Advanced Usage

### Custom Server Configuration
Add your own MCP servers:
```json
{
  "mcp": {
    "servers": {
      "custom-server": {
        "command": "python",
        "args": ["path/to/custom/server.py"],
        "env": {
          "CUSTOM_VAR": "value"
        }
      }
    }
  }
}
```

### Context Customization
```json
{
  "mcp": {
    "context": {
      "includeProjectFiles": true,
      "includeGitHistory": true,
      "includeSystemInfo": true,
      "customContextProviders": ["path/to/provider"]
    }
  }
}
```

## 🎯 Best Practices

1. **Start with Core Servers**: Begin with filesystem and git servers
2. **Monitor Performance**: Watch for memory usage and response times
3. **Regular Updates**: Keep MCP servers updated
4. **Backup Configuration**: Always backup before making changes
5. **Test Incrementally**: Add servers one at a time and test

## 📞 Support

### Logs Location
- **Cursor Logs**: `%APPDATA%\Cursor\logs`
- **MCP Logs**: `H:\Cursor\mcp_server_toolkit\cursor-mcp.log`

### Useful Commands
```powershell
# Check MCP server status
Get-Process node | Select-Object Id, ProcessName, StartTime

# View recent logs
Get-Content "H:\Cursor\mcp_server_toolkit\cursor-mcp.log" -Tail 50

# Test server connectivity
Test-NetConnection localhost -Port 3000
```

## 🎉 Success Indicators

You'll know the integration is working when:
- ✅ Cursor provides more detailed project context
- ✅ AI suggestions reference your actual codebase
- ✅ Git history and commit information is available
- ✅ Docker container information is accessible
- ✅ System resource information is provided

---

**Setup completed successfully!** Your Cursor AI now has enhanced capabilities through MCP integration. 🚀 