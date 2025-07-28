# MCP Toolkit Copy Guide

## Overview

This guide explains how to copy the MCP server toolkit functionality from this project to any other project, enabling the same MCP server management, web interfaces, and automatic startup capabilities.

## Quick Start

### Method 1: Using the Batch File (Easiest)

```bash
# Copy complete MCP toolkit to a new project
copy-mcp-toolkit.bat "H:\Cursor\my_new_project"

# Copy minimal MCP toolkit (core functionality only)
copy-mcp-toolkit.bat "H:\Cursor\my_new_project" -Minimal

# Copy without verification (faster)
copy-mcp-toolkit.bat "H:\Cursor\my_new_project" -SkipVerification
```

### Method 2: Using PowerShell Directly

```powershell
# Copy complete MCP toolkit
.\copy-mcp-toolkit-to-project.ps1 -TargetProjectPath "H:\Cursor\my_new_project"

# Copy minimal MCP toolkit
.\copy-mcp-toolkit-to-project.ps1 -TargetProjectPath "H:\Cursor\my_new_project" -Minimal

# Copy with custom MCP toolkit path
.\copy-mcp-toolkit-to-project.ps1 -TargetProjectPath "H:\Cursor\my_new_project" -MCPToolkitPath "D:\MyMCPToolkit"
```

## Installation Types

### Complete Installation (Default)
Copies all MCP toolkit components including:
- ✅ Core MCP server management
- ✅ Project startup system
- ✅ Auto-shutdown system
- ✅ Web interfaces
- ✅ Configuration files
- ✅ Documentation

**Files copied (16 total):**
- `start-mcp-servers.ps1`
- `stop-mcp-servers.ps1`
- `check-mcp-status.ps1`
- `mcp-project-config.json`
- `start-project-with-mcp.ps1`
- `start-project.bat`
- `cursor-project-startup.ps1`
- `auto-shutdown-mcp.ps1`
- `setup-cursor-auto-shutdown.ps1`
- `start-auto-shutdown-monitor.ps1`
- `mcp-dashboard-server.js`
- `cursor-mcp-integration-config.json`
- `cursor-mcp-extension-config.json`
- `MCP_TOOLKIT_README.md`
- `CURSOR_MCP_INTEGRATION_GUIDE.md`

### Minimal Installation
Copies only core MCP functionality:
- ✅ Core MCP server management
- ✅ Basic web interface
- ❌ No project startup system
- ❌ No auto-shutdown system

**Files copied (5 total):**
- `start-mcp-servers.ps1`
- `stop-mcp-servers.ps1`
- `check-mcp-status.ps1`
- `mcp-project-config.json`
- `mcp-dashboard-server.js`

## Prerequisites

### Required
- **PowerShell 7** or later
- **Node.js** (for MCP servers)
- **Access to MCP toolkit** at `H:\Cursor\mcp_server_toolkit`

### Optional
- **Administrator privileges** (for auto-startup features)
- **Windows Task Scheduler** (for system boot startup)

## Usage Examples

### Example 1: New Project Setup
```bash
# Create a new project with complete MCP toolkit
copy-mcp-toolkit.bat "H:\Cursor\my_awesome_project"
```

### Example 2: Existing Project Enhancement
```bash
# Add MCP toolkit to existing project
copy-mcp-toolkit.bat "H:\Cursor\existing_project" -SkipVerification
```

### Example 3: Minimal Setup for Testing
```bash
# Quick setup with minimal components
copy-mcp-toolkit.bat "H:\Cursor\test_project" -Minimal -SkipVerification
```

### Example 4: Custom MCP Toolkit Path
```powershell
# Use MCP toolkit from different location
.\copy-mcp-toolkit-to-project.ps1 -TargetProjectPath "H:\Cursor\my_project" -MCPToolkitPath "D:\CustomMCPToolkit"
```

## What the Script Does

### 1. Prerequisites Check
- ✅ Verifies target directory exists (creates if needed)
- ✅ Checks MCP toolkit path availability
- ✅ Validates Node.js installation
- ✅ Confirms PowerShell version

### 2. File Copy Process
- ✅ Copies all essential files to target project
- ✅ Maintains file structure and permissions
- ✅ Logs all copy operations
- ✅ Reports success/error counts

### 3. Configuration Updates
- ✅ Updates MCP toolkit paths in configuration files
- ✅ Adjusts paths for target project location
- ✅ Validates configuration file integrity

### 4. Project-Specific Setup
- ✅ Creates project-specific startup script
- ✅ Generates `start-mcp-toolkit.bat` for easy access
- ✅ Customizes startup messages for target project

### 5. Installation Verification
- ✅ Tests startup script functionality
- ✅ Validates status checking scripts
- ✅ Confirms file integrity
- ✅ Reports installation status

## Post-Installation Usage

### Complete Installation Commands
```bash
# One-click startup (recommended)
.\start-project.bat

# Full startup with logging
.\start-project-with-mcp.ps1

# Cursor-specific startup
.\cursor-project-startup.ps1

# Start MCP servers only
.\start-mcp-servers.ps1

# Stop MCP servers
.\stop-mcp-servers.ps1

# Check status
.\check-mcp-status.ps1
```

### Minimal Installation Commands
```bash
# Start MCP servers
.\start-mcp-servers.ps1

# Stop MCP servers
.\stop-mcp-servers.ps1

# Check status
.\check-mcp-status.ps1
```

### Web Interfaces
- **MCP Web Interface**: http://localhost:3000
- **MCP Dashboard**: http://localhost:3001

## Configuration Options

### Script Parameters
- `-TargetProjectPath` (Required): Path to target project directory
- `-MCPToolkitPath` (Optional): Custom MCP toolkit path (default: `H:\Cursor\mcp_server_toolkit`)
- `-Minimal` (Optional): Install minimal components only
- `-Verbose` (Optional): Enable detailed logging
- `-SkipVerification` (Optional): Skip installation verification

### Configuration Files
The script automatically updates these configuration files:
- `mcp-project-config.json` - Main project configuration
- `cursor-mcp-integration-config.json` - Cursor integration settings

## Troubleshooting

### Common Issues

#### 1. "MCP toolkit path not found"
**Solution**: Update the `-MCPToolkitPath` parameter to point to your MCP toolkit location.

#### 2. "Node.js not found"
**Solution**: Install Node.js from https://nodejs.org/

#### 3. "PowerShell execution policy error"
**Solution**: Run as Administrator and execute:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### 4. "Target directory access denied"
**Solution**: Run the script as Administrator or check folder permissions.

### Log Files
- `mcp-toolkit-copy.log` - Detailed copy process log
- `project-startup.log` - Project startup log (after installation)

### Verification Commands
```bash
# Check if files were copied
dir *.ps1
dir *.bat
dir *.json

# Test startup script
.\start-project-with-mcp.ps1 -SkipMCP

# Check MCP status
.\check-mcp-status.ps1
```

## Advanced Usage

### Custom Installation Script
```powershell
# Create custom installation with specific options
.\copy-mcp-toolkit-to-project.ps1 `
    -TargetProjectPath "H:\Cursor\custom_project" `
    -MCPToolkitPath "D:\CustomMCPToolkit" `
    -Minimal `
    -SkipVerification `
    -Verbose
```

### Batch Processing Multiple Projects
```powershell
# Copy to multiple projects
$projects = @(
    "H:\Cursor\project1",
    "H:\Cursor\project2", 
    "H:\Cursor\project3"
)

foreach ($project in $projects) {
    Write-Host "Installing MCP toolkit to: $project"
    .\copy-mcp-toolkit-to-project.ps1 -TargetProjectPath $project -SkipVerification
}
```

## Integration with Existing Projects

### Git Integration
```bash
# Add MCP toolkit files to version control
git add *.ps1 *.bat *.json *.md
git commit -m "Add MCP server toolkit integration"
```

### CI/CD Integration
```bash
# Install MCP toolkit in CI/CD pipeline
.\copy-mcp-toolkit-to-project.ps1 -TargetProjectPath $env:BUILD_DIR -Minimal -SkipVerification
```

## Support and Documentation

### Documentation Files
- `MCP_TOOLKIT_README.md` - Complete MCP toolkit usage guide
- `CURSOR_MCP_INTEGRATION_GUIDE.md` - Cursor integration guide

### Log Files
- `mcp-toolkit-copy.log` - Copy process details
- `project-startup.log` - Startup process details

### Web Resources
- **MCP Web Interface**: http://localhost:3000 (after startup)
- **MCP Dashboard**: http://localhost:3001 (after startup)

## Success Indicators

### ✅ Successful Installation
- All files copied without errors
- Configuration files updated correctly
- Startup script test passes
- Status script runs successfully
- Web interfaces accessible

### ⚠️ Installation with Warnings
- Some files missing (non-critical)
- Node.js not found (MCP servers won't work)
- MCP toolkit path not found (manual path update needed)

### ❌ Failed Installation
- Target directory creation failed
- Critical files missing
- Configuration update failed
- Verification tests failed

## Next Steps

After successful installation:

1. **Test the installation**:
   ```bash
   .\start-project.bat
   ```

2. **Access web interfaces**:
   - Open http://localhost:3000 for MCP Web Interface
   - Open http://localhost:3001 for MCP Dashboard

3. **Configure Cursor integration** (optional):
   - Follow `CURSOR_MCP_INTEGRATION_GUIDE.md`

4. **Set up auto-startup** (optional):
   ```bash
   .\setup-auto-startup.ps1
   ```

5. **Customize configuration**:
   - Edit `mcp-project-config.json` for project-specific settings

**Result**: Your project now has the same comprehensive MCP server toolkit functionality as the original project! 🚀 