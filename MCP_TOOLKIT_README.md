# 🚀 MCP Server Toolkit Integration

A comprehensive integration system that automatically starts and manages all MCP (Model Context Protocol) servers with web interfaces for easy access and monitoring.

## 📋 Overview

This project provides a complete MCP server toolkit integration that includes:

- **8 MCP Servers** for various operations (filesystem, git, docker, system, etc.)
- **2 Web Interfaces** for browser-based access and management
- **Automatic Startup** with Windows Task Scheduler integration
- **Real-time Monitoring** and server management
- **Cursor Integration** ready for AI tool connectivity

## 🌐 Quick Access

Once started, access your MCP toolkit at:

- **Web Interface**: http://localhost:3000
- **Dashboard**: http://localhost:3001

## 🚀 Quick Start

### 1. Automatic Startup (Recommended)

```powershell
# Run as Administrator
.\setup-auto-startup.ps1
```

This will:
- ✅ Install automatic startup on system boot
- ✅ Configure Windows Task Scheduler
- ✅ Start all MCP servers automatically

### 2. Manual Startup

```powershell
# Start all MCP servers and web interfaces
.\start-mcp-servers.ps1

# Stop all MCP servers
.\stop-mcp-servers.ps1
```

### 3. Test the Setup

```powershell
# Check if servers are running
netstat -an | findstr ":300"

# Check Node.js processes
tasklist | findstr node
```

## 📊 Available MCP Servers

| Server | Category | Auto-Start | Description |
|--------|----------|------------|-------------|
| **filesystem** | Core | ✅ | File system operations and management |
| **git** | Version Control | ✅ | Git repository management |
| **docker** | Containerization | ✅ | Docker container management |
| **system** | System | ✅ | System information and operations |
| **memory** | Performance | ✅ | Memory and cache management |
| **docker-v2** | Containerization | ❌ | Enhanced Docker operations |
| **docker-cli** | Containerization | ❌ | Docker CLI interface |
| **registry** | Management | ✅ | MCP server registry |

## 🌐 Web Services

### Port 3000 - MCP Web Interface
- **Purpose**: Browser-based MCP client
- **Features**: 
  - Connect to MCP servers
  - Execute MCP operations
  - Real-time communication
  - User-friendly interface

### Port 3001 - MCP Dashboard
- **Purpose**: Server management and monitoring
- **Features**:
  - Start/stop individual servers
  - Real-time status monitoring
  - Server statistics
  - Process management

## 🔧 Configuration

### Project Configuration
All settings are stored in `mcp-project-config.json`:

```json
{
  "ports": {
    "webInterface": 3000,
    "dashboard": 3001
  },
  "startup": {
    "autoStart": true,
    "backgroundMode": true
  }
}
```

### Cursor Integration
Ready-to-use Cursor integration files:
- `cursor-mcp-integration-config.json` - Main configuration
- `cursor-mcp-extension-config.json` - Extension manifest

## 📁 File Structure

```
H:\Cursor\delete_windows_apps\
├── start-mcp-servers.ps1          # Main startup script
├── stop-mcp-servers.ps1           # Stop script
├── setup-auto-startup.ps1         # Auto-startup installer
├── mcp-project-config.json        # Project configuration
├── mcp-dashboard-server.js        # Dashboard server (port 3001)
├── cursor-mcp-integration-config.json
├── cursor-mcp-extension-config.json
└── MCP_TOOLKIT_README.md          # This file

H:\Cursor\mcp_server_toolkit\      # MCP server files
├── filesystem-mcp-server.js
├── git-mcp-server.js
├── docker-mcp-server.js
├── system-mcp-server.js
├── memory-mcp-server.js
├── docker-mcp-server-v2.js
├── docker-cli-server.js
├── mcp-server-registry.js
├── mcp-web-interface.js           # Web interface (port 3000)
└── mcp-dashboard-server.js        # Dashboard server
```

## 🎛️ Management Commands

### Start Services
```powershell
# Start all services
.\start-mcp-servers.ps1

# Start with verbose output
.\start-mcp-servers.ps1 -Verbose

# Start in foreground mode
.\start-mcp-servers.ps1 -Background:$false
```

### Stop Services
```powershell
# Graceful stop
.\stop-mcp-servers.ps1

# Force stop
.\stop-mcp-servers.ps1 -Force

# Verbose stop
.\stop-mcp-servers.ps1 -Verbose
```

### Auto-Startup Management
```powershell
# Install auto-startup (requires admin)
.\setup-auto-startup.ps1

# Uninstall auto-startup
.\setup-auto-startup.ps1 -Uninstall

# Check status
Get-ScheduledTask -TaskName "MCP-Server-Toolkit-Startup"
```

### Auto-Shutdown for Cursor
```powershell
# Install auto-shutdown monitor (requires admin)
.\setup-cursor-auto-shutdown.ps1 -Install

# Uninstall auto-shutdown
.\setup-cursor-auto-shutdown.ps1 -Uninstall

# Manually start auto-shutdown monitor
.\start-auto-shutdown-monitor.ps1

# Check auto-shutdown status
.\setup-cursor-auto-shutdown.ps1
```

**What it does:**
- Monitors Cursor process automatically
- Stops MCP servers (ports 3000, 3001) when Cursor exits
- Prevents orphaned Node.js processes
- Logs all shutdown activities

## 🔍 Monitoring and Status

### Check Running Services
```powershell
# Check ports
netstat -an | findstr ":300"

# Check processes
tasklist | findstr node

# Check specific ports
Test-NetConnection -ComputerName localhost -Port 3000
Test-NetConnection -ComputerName localhost -Port 3001
```

### Dashboard Features
- **Real-time Status**: Live server status updates
- **Start/Stop Controls**: Individual server management
- **Statistics**: Running count, uptime, memory usage
- **Auto-refresh**: Updates every 5 seconds

## 🔗 Integration Options

### 1. Cursor Integration
The project includes ready-to-use Cursor integration:
- Automatic MCP server discovery
- Seamless AI tool connectivity
- Configuration management

### 2. Web Interface
Access via browser for:
- MCP client operations
- Server communication
- Real-time interactions

### 3. Dashboard Management
Centralized management for:
- Server lifecycle control
- Status monitoring
- Performance tracking

## 🛠️ Troubleshooting

### Common Issues

#### Port Already in Use
```powershell
# Check what's using the port
netstat -ano | findstr ":3000"
netstat -ano | findstr ":3001"

# Kill process by PID
taskkill /PID <PID> /F
```

#### Node.js Processes Not Starting
```powershell
# Check Node.js installation
node --version

# Check file permissions
Get-Acl "H:\Cursor\mcp_server_toolkit\*.js"

# Run with verbose output
.\start-mcp-servers.ps1 -Verbose
```

#### Auto-Startup Not Working
```powershell
# Check task scheduler
Get-ScheduledTask -TaskName "MCP-Server-Toolkit-Startup"

# Reinstall auto-startup
.\setup-auto-startup.ps1 -Uninstall
.\setup-auto-startup.ps1
```

### Logs and Debugging
- Check PowerShell output for error messages
- Monitor Node.js process logs
- Use `-Verbose` flag for detailed output

## 🔒 Security Considerations

- **Admin Privileges**: Required for auto-startup installation
- **Local Access**: Services run on localhost only
- **Process Isolation**: Each server runs in separate process
- **Graceful Shutdown**: Proper cleanup on stop

## 📈 Performance

### Resource Usage
- **Memory**: ~50MB per Node.js process
- **CPU**: Minimal usage for idle servers
- **Network**: Local communication only
- **Startup Time**: ~10-15 seconds for all services

### Optimization
- Background mode reduces resource usage
- Auto-refresh intervals are configurable
- Process management includes cleanup

## 🔄 Updates and Maintenance

### Updating Servers
1. Update files in `H:\Cursor\mcp_server_toolkit\`
2. Restart services: `.\stop-mcp-servers.ps1 && .\start-mcp-servers.ps1`

### Configuration Changes
1. Edit `mcp-project-config.json`
2. Restart services to apply changes

### Adding New Servers
1. Add server file to MCP toolkit directory
2. Update configuration in `mcp-project-config.json`
3. Update startup script if needed

## 📞 Support

### Quick Commands
```powershell
# Status check
.\start-mcp-servers.ps1 -Verbose

# Full restart
.\stop-mcp-servers.ps1 -Force
.\start-mcp-servers.ps1

# Reset auto-startup
.\setup-auto-startup.ps1 -Uninstall
.\setup-auto-startup.ps1
```

### Documentation Files
- `MCP_USAGE_INSTRUCTIONS.md` - Detailed server usage
- `CURSOR_MCP_INTEGRATION_GUIDE.md` - Integration guide
- `BACKUP_TROUBLESHOOTING.md` - Troubleshooting tips

## 🎉 Success Indicators

✅ **All systems operational when:**
- Port 3000 responds to http://localhost:3000
- Port 3001 responds to http://localhost:3001
- 4+ Node.js processes are running
- Dashboard shows all servers as "running"

---

**🚀 Your MCP Server Toolkit is now fully integrated and ready for use!** 