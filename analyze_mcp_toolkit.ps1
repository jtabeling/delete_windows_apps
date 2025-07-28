# MCP Server Toolkit Analyzer
# This script automatically analyzes your mcp_server_toolkit project

Write-Host "=== MCP Server Toolkit Analyzer ===" -ForegroundColor Cyan
Write-Host "Analyzing your MCP server toolkit project..." -ForegroundColor Yellow

# Function to find mcp_server_toolkit project
function Find-MCPServerToolkit {
    Write-Host "`n🔍 Searching for mcp_server_toolkit project..." -ForegroundColor Cyan
    
    # Common locations to search
    $searchPaths = @(
        "H:\Cursor",
        "$env:USERPROFILE\Documents",
        "$env:USERPROFILE\Desktop", 
        "$env:USERPROFILE\Projects",
        "$env:USERPROFILE\Code",
        "$env:USERPROFILE\GitHub",
        "$env:USERPROFILE\Source",
        "C:\Projects",
        "C:\Code",
        "C:\GitHub"
    )
    
    $foundProjects = @()
    
    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            $toolkitPath = Join-Path $path "mcp_server_toolkit"
            if (Test-Path $toolkitPath) {
                $foundProjects += $toolkitPath
                Write-Host "✅ Found: $toolkitPath" -ForegroundColor Green
            }
        }
    }
    
    # Also search in current directory and subdirectories
    $currentDirToolkit = Get-ChildItem -Path . -Name "mcp_server_toolkit" -Directory -Recurse -ErrorAction SilentlyContinue
    foreach ($toolkit in $currentDirToolkit) {
        $fullPath = Resolve-Path $toolkit
        if ($fullPath -notin $foundProjects) {
            $foundProjects += $fullPath
            Write-Host "✅ Found: $fullPath" -ForegroundColor Green
        }
    }
    
    return $foundProjects
}

# Function to analyze project structure
function Analyze-ProjectStructure {
    param([string]$ProjectPath)
    
    Write-Host "`n📁 Analyzing project structure..." -ForegroundColor Cyan
    Write-Host "Project: $ProjectPath" -ForegroundColor Yellow
    
    try {
        $structure = @{
            Path = $ProjectPath
            Files = @()
            Directories = @()
            ServerFiles = @()
            ConfigFiles = @()
            ReadmeFiles = @()
            LanguageFiles = @()
        }
        
        # Get all files and directories
        $allItems = Get-ChildItem -Path $ProjectPath -Recurse -Force -ErrorAction SilentlyContinue
        
        foreach ($item in $allItems) {
            if ($item.PSIsContainer) {
                $structure.Directories += $item.FullName
            } else {
                $structure.Files += $item.FullName
                
                # Categorize files
                $fileName = $item.Name.ToLower()
                $extension = $item.Extension.ToLower()
                
                # Server files
                if ($fileName -match "server|mcp" -and $extension -in @(".py", ".js", ".ts", ".go", ".rs", ".java", ".cs")) {
                    $structure.ServerFiles += $item.FullName
                }
                
                # Config files
                if ($extension -in @(".json", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".conf")) {
                    $structure.ConfigFiles += $item.FullName
                }
                
                # Readme files
                if ($fileName -match "readme|read_me|documentation") {
                    $structure.ReadmeFiles += $item.FullName
                }
                
                # Language-specific files
                switch ($extension) {
                    ".py" { $structure.LanguageFiles += @{Type="Python"; File=$item.FullName} }
                    ".js" { $structure.LanguageFiles += @{Type="JavaScript"; File=$item.FullName} }
                    ".ts" { $structure.LanguageFiles += @{Type="TypeScript"; File=$item.FullName} }
                    ".go" { $structure.LanguageFiles += @{Type="Go"; File=$item.FullName} }
                    ".rs" { $structure.LanguageFiles += @{Type="Rust"; File=$item.FullName} }
                    ".java" { $structure.LanguageFiles += @{Type="Java"; File=$item.FullName} }
                    ".cs" { $structure.LanguageFiles += @{Type="C#"; File=$item.FullName} }
                }
            }
        }
        
        return $structure
    }
    catch {
        Write-Host "❌ Error analyzing project structure: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to identify MCP servers
function Identify-MCPServers {
    param([array]$ServerFiles)
    
    Write-Host "`n🔧 Identifying MCP servers..." -ForegroundColor Cyan
    
    $servers = @()
    
    foreach ($file in $ServerFiles) {
        try {
            $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
            $fileName = Split-Path $file -Leaf
            
            $serverInfo = @{
                File = $file
                Name = $fileName
                Type = "Unknown"
                Language = ""
                HasMCPProtocol = $false
                HasServerClass = $false
                Dependencies = @()
                Config = @{}
            }
            
            # Determine language
            $extension = [System.IO.Path]::GetExtension($file).ToLower()
            switch ($extension) {
                ".py" { $serverInfo.Language = "Python" }
                ".js" { $serverInfo.Language = "JavaScript" }
                ".ts" { $serverInfo.Language = "TypeScript" }
                ".go" { $serverInfo.Language = "Go" }
                ".rs" { $serverInfo.Language = "Rust" }
                ".java" { $serverInfo.Language = "Java" }
                ".cs" { $serverInfo.Language = "C#" }
            }
            
            # Check for MCP protocol indicators
            if ($content -match "mcp|ModelContextProtocol|jsonrpc|2\.0") {
                $serverInfo.HasMCPProtocol = $true
            }
            
            # Check for server class/function
            if ($content -match "class.*Server|def.*server|func.*server|server.*class") {
                $serverInfo.HasServerClass = $true
            }
            
            # Determine server type based on content
            if ($content -match "database|schema|sql|postgres|mysql") {
                $serverInfo.Type = "Database Schema Server"
            }
            elseif ($content -match "file|filesystem|directory|folder") {
                $serverInfo.Type = "File System Server"
            }
            elseif ($content -match "git|repository|commit|history") {
                $serverInfo.Type = "Git History Server"
            }
            elseif ($content -match "api|endpoint|http|rest") {
                $serverInfo.Type = "API Documentation Server"
            }
            elseif ($content -match "documentation|docs|readme") {
                $serverInfo.Type = "Documentation Server"
            }
            else {
                $serverInfo.Type = "Generic MCP Server"
            }
            
            # Extract dependencies
            if ($serverInfo.Language -eq "Python") {
                $imports = [regex]::Matches($content, "import\s+(\w+)|from\s+(\w+)\s+import")
                foreach ($match in $imports) {
                    $serverInfo.Dependencies += $match.Groups[1].Value
                }
            }
            elseif ($serverInfo.Language -eq "JavaScript" -or $serverInfo.Language -eq "TypeScript") {
                $requires = [regex]::Matches($content, "require\(['""]([^'""]+)['""]\)|import.*from\s+['""]([^'""]+)['""]")
                foreach ($match in $requires) {
                    $serverInfo.Dependencies += $match.Groups[1].Value
                }
            }
            
            $servers += $serverInfo
            
        }
        catch {
            Write-Host "⚠️  Error analyzing server file $file : $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    return $servers
}

# Function to analyze configuration files
function Analyze-ConfigFiles {
    param([array]$ConfigFiles)
    
    Write-Host "`n⚙️  Analyzing configuration files..." -ForegroundColor Cyan
    
    $configs = @()
    
    foreach ($file in $ConfigFiles) {
        try {
            $extension = [System.IO.Path]::GetExtension($file).ToLower()
            $fileName = Split-Path $file -Leaf
            
            $configInfo = @{
                File = $file
                Name = $fileName
                Type = $extension
                Content = $null
                IsValid = $false
            }
            
            # Parse different config file types
            switch ($extension) {
                ".json" {
                    try {
                        $configInfo.Content = Get-Content $file -Raw | ConvertFrom-Json
                        $configInfo.IsValid = $true
                    }
                    catch {
                        Write-Host "⚠️  Invalid JSON in $fileName" -ForegroundColor Yellow
                    }
                }
                ".yaml" {
                    try {
                        # Basic YAML parsing (you might need a YAML parser)
                        $configInfo.Content = Get-Content $file -Raw
                        $configInfo.IsValid = $true
                    }
                    catch {
                        Write-Host "⚠️  Error reading YAML file $fileName" -ForegroundColor Yellow
                    }
                }
                ".toml" {
                    try {
                        $configInfo.Content = Get-Content $file -Raw
                        $configInfo.IsValid = $true
                    }
                    catch {
                        Write-Host "⚠️  Error reading TOML file $fileName" -ForegroundColor Yellow
                    }
                }
            }
            
            $configs += $configInfo
            
        }
        catch {
            Write-Host "⚠️  Error analyzing config file $file : $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    return $configs
}

# Function to generate usage instructions
function Generate-UsageInstructions {
    param([object]$Structure, [array]$Servers, [array]$Configs)
    
    Write-Host "`n📋 Generating usage instructions..." -ForegroundColor Cyan
    
    $instructions = @"
# MCP Server Toolkit Usage Instructions

## Project Overview
- **Location**: $($Structure.Path)
- **Total Files**: $($Structure.Files.Count)
- **Total Directories**: $($Structure.Directories.Count)
- **MCP Servers Found**: $($Servers.Count)
- **Configuration Files**: $($Configs.Count)

## Available MCP Servers

"@
    
    foreach ($server in $Servers) {
        $instructions += @"

### $($server.Name)
- **Type**: $($server.Type)
- **Language**: $($server.Language)
- **File**: $($server.File)
- **MCP Protocol**: $(if($server.HasMCPProtocol) {"✅ Yes"} else {"❌ No"})
- **Server Class**: $(if($server.HasServerClass) {"✅ Yes"} else {"❌ No"})

#### How to Run:
"@
        
        switch ($server.Language) {
            "Python" {
                $instructions += @"
```bash
cd $($Structure.Path)
python $($server.File)
```
"@
            }
            "JavaScript" {
                $instructions += @"
```bash
cd $($Structure.Path)
node $($server.File)
```
"@
            }
            "TypeScript" {
                $instructions += @"
```bash
cd $($Structure.Path)
npx ts-node $($server.File)
```
"@
            }
            "Go" {
                $instructions += @"
```bash
cd $($Structure.Path)
go run $($server.File)
```
"@
            }
            "Rust" {
                $instructions += @"
```bash
cd $($Structure.Path)
cargo run --bin $($server.Name.Replace('.rs', ''))
```
"@
            }
        }
        
        if ($server.Dependencies.Count -gt 0) {
            $instructions += @"

#### Dependencies:
$($server.Dependencies -join ', ')
"@
        }
    }
    
    $instructions += @"

## Configuration Files
"@
    
    foreach ($config in $Configs) {
        $instructions += @"

### $($config.Name)
- **Type**: $($config.Type)
- **Valid**: $(if($config.IsValid) {"✅ Yes"} else {"❌ No"})
- **File**: $($config.File)
"@
    }
    
    $instructions += @"

## Integration with Cursor/AI Tools

### Option 1: Direct Server Execution
Run servers directly and connect via MCP protocol.

### Option 2: Configuration-Based Integration
Create a configuration file for your AI tool:

```json
{
  "mcpServers": {
"@
    
    foreach ($server in $Servers) {
        $serverName = $server.Name.Replace('.py', '').Replace('.js', '').Replace('.ts', '').Replace('.go', '').Replace('.rs', '')
        $instructions += @"
    "$serverName": {
      "command": "$(switch($server.Language) {'Python' {'python'}; 'JavaScript' {'node'}; 'TypeScript' {'npx ts-node'}; 'Go' {'go run'}; 'Rust' {'cargo run --bin'}})",
      "args": ["$($server.File)"],
      "env": {
        "PROJECT_ROOT": "$($Structure.Path)"
      }
    },
"@
    }
    
    $instructions += @"
  }
}
```

## Next Steps
1. Test each server individually
2. Configure your AI tool to connect to these servers
3. Verify MCP protocol communication
4. Customize server configurations as needed

## Troubleshooting
- Ensure all dependencies are installed
- Check server logs for connection issues
- Verify MCP protocol compliance
- Test with MCP client tools
"@
    
    return $instructions
}

# Main execution
try {
    # Find MCP server toolkit projects
    $projects = Find-MCPServerToolkit
    
    if ($projects.Count -eq 0) {
        Write-Host "❌ No mcp_server_toolkit projects found!" -ForegroundColor Red
        Write-Host "💡 Please ensure your project is named 'mcp_server_toolkit' and is in a common location." -ForegroundColor Yellow
        exit 1
    }
    
    # Analyze each project
    foreach ($project in $projects) {
        Write-Host "`n" + "="*60 -ForegroundColor Gray
        Write-Host "ANALYZING PROJECT: $project" -ForegroundColor Cyan
        Write-Host "="*60 -ForegroundColor Gray
        
        # Analyze project structure
        $structure = Analyze-ProjectStructure -ProjectPath $project
        
        if ($structure) {
            # Identify MCP servers
            $servers = Identify-MCPServers -ServerFiles $structure.ServerFiles
            
            # Analyze configuration files
            $configs = Analyze-ConfigFiles -ConfigFiles $structure.ConfigFiles
            
            # Generate usage instructions
            $instructions = Generate-UsageInstructions -Structure $structure -Servers $servers -Configs $configs
            
            # Save instructions to file
            $outputFile = Join-Path $project "MCP_USAGE_INSTRUCTIONS.md"
            $instructions | Out-File -FilePath $outputFile -Encoding UTF8
            
            Write-Host "`n✅ Analysis complete!" -ForegroundColor Green
            Write-Host "📄 Usage instructions saved to: $outputFile" -ForegroundColor Cyan
            
            # Display summary
            Write-Host "`n📊 SUMMARY:" -ForegroundColor Cyan
            Write-Host "   • MCP Servers found: $($servers.Count)" -ForegroundColor White
            Write-Host "   • Configuration files: $($configs.Count)" -ForegroundColor White
            Write-Host "   • Languages used: $(($servers | Select-Object -ExpandProperty Language | Sort-Object -Unique) -join ', ')" -ForegroundColor White
            
            if ($servers.Count -gt 0) {
                Write-Host "`n🚀 READY TO USE SERVERS:" -ForegroundColor Green
                foreach ($server in $servers) {
                    Write-Host "   • $($server.Name) ($($server.Type))" -ForegroundColor White
                }
            }
        }
    }
    
    Write-Host "`n🎉 Analysis complete! Check the generated MCP_USAGE_INSTRUCTIONS.md files for detailed usage information." -ForegroundColor Green
    
}
catch {
    Write-Host "❌ Error during analysis: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Gray
}

Write-Host "`nPress any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 