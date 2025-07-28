# Technical Context: WindowsApps Manager + MCP Toolkit Integration

## Technology Stack

### WindowsAppsManager (Original Project)
- **Framework**: .NET 5.0 Windows Forms
- **Language**: C#
- **Platform**: Windows 10/11
- **Architecture**: Desktop Application with Administrator privileges
- **UI Framework**: Windows Forms with DataGridView, ProgressBar, Context Menus
- **File Operations**: System.IO with async/await patterns
- **Registry Operations**: Microsoft.Win32.Registry
- **PowerShell Integration**: System.Management.Automation
- **Security**: Windows Defender Controlled Folder Access integration

### MCP Toolkit Integration (New Addition)
- **Runtime**: Node.js
- **Language**: JavaScript/TypeScript
- **Protocol**: MCP (Model Context Protocol) 1.0
- **Communication**: stdio, HTTP, WebSocket
- **Web Framework**: Express.js
- **Frontend**: HTML5, CSS3, JavaScript (Vanilla)
- **Process Management**: PowerShell scripts for Windows integration
- **Scheduling**: Windows Task Scheduler
- **Configuration**: JSON-based configuration management

### Project Startup System (Latest Addition)
- **Scripting**: PowerShell 7 with enhanced capabilities
- **Batch Processing**: Windows batch files for one-click operation
- **Process Management**: Intelligent detection and management of running services
- **Logging**: Comprehensive timestamped logging with color-coded output
- **Integration**: Seamless integration with MCP toolkit and auto-shutdown systems

## Development Environment

### Core Requirements
- **Windows 10/11**: Primary target platform
- **Administrator Privileges**: Required for WindowsAppsManager operations
- **Node.js**: Required for MCP server toolkit (v14+ recommended)
- **PowerShell 7**: Enhanced scripting capabilities
- **Visual Studio 2019/2022**: .NET development (optional)
- **Git**: Version control and repository management

### Project Structure
```
H:\Cursor\delete_windows_apps\          # Main project directory
├── WindowsAppsManager.csproj           # .NET project file
├── Program.cs                          # Application entry point
├── Forms/                              # Windows Forms UI components
├── Services/                           # Business logic services
├── Models/                             # Data models
├── Utils/                              # Utility classes
├── start-project-with-mcp.ps1         # Main project startup script
├── start-project.bat                  # One-click startup batch file
├── cursor-project-startup.ps1         # Cursor-specific startup script
├── start-mcp-servers.ps1              # MCP server startup script
├── stop-mcp-servers.ps1               # MCP server shutdown script
├── setup-auto-startup.ps1             # Auto-startup configuration
├── auto-shutdown-mcp.ps1              # Auto-shutdown monitor
├── setup-cursor-auto-shutdown.ps1     # Auto-shutdown setup
├── start-auto-shutdown-monitor.ps1    # Manual auto-shutdown start
├── check-mcp-status.ps1               # Status verification script
├── mcp-project-config.json            # MCP toolkit configuration
├── mcp-dashboard-server.js            # Dashboard web server
├── cursor-mcp-integration-config.json # Cursor integration config
├── cursor-mcp-extension-config.json   # Extension manifest
├── MCP_TOOLKIT_README.md              # MCP toolkit documentation
└── memory-bank/                       # Project documentation

H:\Cursor\mcp_server_toolkit\          # MCP server files
├── filesystem-mcp-server.js           # File system operations
├── git-mcp-server.js                  # Git repository management
├── docker-mcp-server.js               # Docker container management
├── system-mcp-server.js               # System information
├── memory-mcp-server.js               # Memory and cache management
├── docker-mcp-server-v2.js            # Enhanced Docker operations
├── docker-cli-server.js               # Docker CLI interface
├── mcp-server-registry.js             # MCP server registry
├── mcp-web-interface.js               # Web interface (port 3000)
└── mcp-dashboard-server.js            # Dashboard server (port 3001)
```

## Key Technical Components

### WindowsAppsManager Core Components

#### Models
- **WindowsApp**: Complete app data representation with safety methods
- **BackupInfo**: Backup metadata and tracking with crash prevention
- **RegistryBackupInfo**: Registry backup state management
- **BackupStatus**: Comprehensive backup state enumeration

#### Services
- **AppService**: WindowsApps discovery with manifest parsing
- **BackupService**: Enhanced with crash prevention, memory management, Windows Defender integration
- **DeletionService**: Advanced multi-phase deletion with intelligent cleanup

#### Forms
- **MainForm**: Professional interface with comprehensive functionality
- **ConfirmationDialog**: Multi-level safety confirmation with risk assessment
- **ProgressForm**: Real-time operation progress tracking
- **BackupSelectionDialog**: Backup management with restore capabilities

#### Utilities
- **PermissionHelper**: Administrator privilege validation
- **BackupDiagnostic**: Diagnostic tool for backup testing and troubleshooting
- **Fix-ControlledFolderAccess.ps1**: Automated Windows Defender integration
- **CheckDeletionLogs.ps1**: Deletion operation monitoring and analysis

### MCP Toolkit Components

#### Management Scripts
- **start-mcp-servers.ps1**: Main startup script with intelligent server management
- **stop-mcp-servers.ps1**: Graceful shutdown with process cleanup
- **setup-auto-startup.ps1**: Windows Task Scheduler integration
- **check-mcp-status.ps1**: Quick status verification script

#### Configuration
- **mcp-project-config.json**: Comprehensive configuration management
- **cursor-mcp-integration-config.json**: Cursor integration configuration
- **cursor-mcp-extension-config.json**: Extension manifest for Cursor

#### Web Services
- **mcp-dashboard-server.js**: Web dashboard for server management (port 3001)
- **mcp-web-interface.js**: MCP web interface (port 3000)

#### MCP Servers
- **filesystem-mcp-server.js**: File system operations and management
- **git-mcp-server.js**: Git repository management and operations
- **docker-mcp-server.js**: Docker container management and operations
- **system-mcp-server.js**: System information and operations
- **memory-mcp-server.js**: Memory and cache management
- **docker-mcp-server-v2.js**: Enhanced Docker operations with advanced features
- **docker-cli-server.js**: Docker CLI interface and command execution
- **mcp-server-registry.js**: MCP server registry and discovery

### Project Startup System Components

#### Startup Scripts
- **start-project-with-mcp.ps1**: Main project startup script with full logging and status monitoring
- **start-project.bat**: Simple batch file for one-click startup (double-click to run)
- **cursor-project-startup.ps1**: Lightweight Cursor-specific startup script

#### Auto-Shutdown System
- **auto-shutdown-mcp.ps1**: Main auto-shutdown monitor script
- **setup-cursor-auto-shutdown.ps1**: Auto-shutdown setup for Task Scheduler
- **start-auto-shutdown-monitor.ps1**: Manual auto-shutdown monitor start

#### Integration Features
- **Enhanced Logging System**: Timestamped entries with color-coded output types
- **Port Verification System**: Checks if servers are already running on ports 3000 and 3001
- **Intelligent Detection**: Avoids duplicate startup of already running servers
- **Status Reporting**: Real-time status with access URLs and port verification

## Technical Architecture

### WindowsAppsManager Architecture
- **Single-threaded UI**: Windows Forms with async/await for responsiveness
- **Service Layer Pattern**: Separation of concerns with dedicated services
- **Error Isolation**: File-level error handling preventing cascade failures
- **Memory Management**: Proactive garbage collection for large operations
- **Security Integration**: Windows Defender Controlled Folder Access coordination

### MCP Toolkit Architecture
- **Multi-process Design**: Each MCP server runs in separate Node.js process
- **Web Interface Layer**: HTTP-based dashboard and web interface
- **Configuration-Driven**: Centralized JSON configuration for all settings
- **Process Management**: PowerShell scripts for Windows integration
- **Real-time Monitoring**: Live status tracking and health checks

### Project Startup Architecture
- **One-Click Operation**: Simple double-click to start entire project
- **Intelligent Detection**: Checks if MCP servers are already running before starting
- **Comprehensive Logging**: Timestamped entries with color-coded output types
- **Port Verification**: Confirms ports 3000 and 3001 are active after startup
- **Auto-shutdown Integration**: Automatically starts monitor when servers start
- **Status Reporting**: Shows access URLs and current server status

### Integration Architecture
- **Modular Design**: Separate components for different management functions
- **Configuration Management**: Centralized settings for easy customization
- **Error Resilience**: Comprehensive error handling and recovery
- **Documentation**: Complete guides and usage instructions
- **Seamless Integration**: Zero manual intervention required for MCP server toolkit operation

## Communication Protocols

### WindowsAppsManager
- **File System**: Direct file operations with System.IO
- **Registry**: Windows Registry access via Microsoft.Win32.Registry
- **PowerShell**: System.Management.Automation for Windows app management
- **Inter-process**: Standard .NET process communication

### MCP Toolkit
- **stdio**: Standard input/output for MCP server communication
- **HTTP**: Web interface and dashboard communication
- **WebSocket**: Real-time updates and status monitoring
- **JSON-RPC 2.0**: MCP protocol implementation

### Project Startup System
- **PowerShell**: Enhanced scripting with parameter handling and error management
- **Batch Processing**: Windows batch file execution for one-click operation
- **Process Communication**: Inter-process communication for status checking
- **Logging**: File-based logging with structured output

## Security Considerations

### WindowsAppsManager
- **Administrator Privileges**: Required for WindowsApps folder access
- **Windows Defender Integration**: Controlled Folder Access coordination
- **Permission Management**: Progressive permission escalation
- **Integrity Protection**: Windows app database integrity safeguards

### MCP Toolkit
- **Local Access Only**: Services run on localhost for security
- **Process Isolation**: Each server runs in separate process
- **Configuration Security**: JSON configuration with validation
- **Web Interface Security**: CORS enabled for local development

### Project Startup System
- **Local Execution**: All scripts run locally with user permissions
- **Process Monitoring**: Safe monitoring of Cursor process for auto-shutdown
- **Error Handling**: Graceful handling of startup failures and missing scripts
- **Logging Security**: Local file logging with user-controlled access

## Performance Characteristics

### WindowsAppsManager
- **Memory Usage**: ~50-100MB for typical operations
- **Startup Time**: ~2-3 seconds
- **Backup Operations**: Memory management prevents crashes on large apps
- **Deletion Operations**: Multi-phase with intelligent cleanup

### MCP Toolkit
- **Memory Usage**: ~50MB per Node.js process
- **Startup Time**: ~10-15 seconds for all services
- **Web Interface**: Real-time updates every 5 seconds
- **Process Management**: Background mode for minimal resource usage

### Project Startup System
- **Startup Time**: ~5-10 seconds for complete project startup
- **Memory Usage**: Minimal overhead for startup scripts
- **Detection Speed**: ~2-3 seconds for server status verification
- **Logging Performance**: Efficient timestamped logging with minimal impact

## Deployment Requirements

### WindowsAppsManager
- **.NET 5.0 Runtime**: Required for application execution
- **Windows 10/11**: Target operating system
- **Administrator Access**: Required for all operations
- **Windows Defender**: Compatible with Controlled Folder Access

### MCP Toolkit
- **Node.js**: Required for MCP server execution
- **PowerShell 7**: Enhanced scripting capabilities
- **Windows Task Scheduler**: For automatic startup
- **Web Browser**: For dashboard and web interface access

### Project Startup System
- **PowerShell 7**: Enhanced scripting capabilities
- **Windows Batch**: For one-click startup operation
- **File System Access**: For logging and status file creation
- **Process Management**: For server detection and monitoring

## Development Workflow

### WindowsAppsManager Development
1. **C# Development**: Visual Studio or VS Code with C# extension
2. **Windows Forms Design**: Visual designer for UI components
3. **PowerShell Testing**: Script-based testing and validation
4. **Integration Testing**: Windows Defender and security testing

### MCP Toolkit Development
1. **Node.js Development**: JavaScript/TypeScript development
2. **PowerShell Scripting**: Windows integration and automation
3. **Web Development**: HTML/CSS/JavaScript for dashboard
4. **Configuration Management**: JSON-based settings and server definitions

### Project Startup Development
1. **PowerShell Scripting**: Enhanced scripting with parameter handling
2. **Batch File Creation**: One-click startup file development
3. **Integration Testing**: End-to-end testing of startup workflow
4. **Logging Implementation**: Comprehensive activity tracking

### Combined Development
1. **Triple Project Management**: All three systems in same workspace
2. **Integration Testing**: End-to-end testing of all systems
3. **Documentation**: Comprehensive guides for all projects
4. **Deployment**: Coordinated deployment of all systems

## Technical Constraints

### WindowsAppsManager
- **Windows Only**: No cross-platform support
- **Administrator Required**: Cannot run without elevated privileges
- **WindowsApps Folder**: Specific to Windows Store app management
- **PowerShell Dependency**: Requires PowerShell for app operations

### MCP Toolkit
- **Node.js Dependency**: Requires Node.js runtime
- **Local Network**: Web interfaces only accessible locally
- **Process Management**: Multiple Node.js processes required
- **Configuration Complexity**: JSON configuration management

### Project Startup System
- **Windows Only**: PowerShell and batch files are Windows-specific
- **PowerShell 7**: Requires enhanced PowerShell capabilities
- **Local Execution**: All scripts must run locally
- **Process Dependencies**: Requires MCP toolkit to be available

## Future Technical Considerations

### WindowsAppsManager
- **.NET 6+ Migration**: Potential framework upgrade
- **Cross-platform**: Potential Linux/macOS support
- **Cloud Integration**: Potential cloud backup options
- **Advanced Security**: Enhanced security features

### MCP Toolkit
- **Docker Integration**: Containerized deployment options
- **Cloud Deployment**: Remote server management
- **Advanced Monitoring**: Enhanced health monitoring
- **API Integration**: REST API for external integrations

### Project Startup System
- **Cross-platform**: Potential Linux/macOS startup scripts
- **Advanced Monitoring**: Enhanced process monitoring capabilities
- **Cloud Integration**: Potential cloud-based startup management
- **API Integration**: REST API for remote startup control

## Technical Excellence Achievements

### WindowsAppsManager
- **Zero Crashes**: Comprehensive error handling and memory management
- **Complete Cleanup**: Multi-phase deletion with intelligent fallback
- **Security Integration**: Seamless Windows Defender compatibility
- **Professional UX**: Detailed progress reporting and risk assessment

### MCP Toolkit
- **Automatic Management**: Complete server lifecycle management
- **Real-time Monitoring**: Live status tracking and health checks
- **Web Interface**: Modern dashboard for server management
- **Integration Ready**: Complete Cursor and AI tool connectivity

### Project Startup System
- **One-Click Operation**: Simple double-click to start entire project
- **Intelligent Detection**: Avoids duplicate startup of already running servers
- **Comprehensive Logging**: Detailed activity tracking with timestamps
- **Seamless Integration**: Zero manual intervention required for MCP server toolkit operation

**Result**: A comprehensive technical platform combining Windows app management with MCP server toolkit integration and automatic project startup, providing local system management, AI tool connectivity capabilities, and seamless project integration with zero manual intervention required. 