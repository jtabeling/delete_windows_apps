# MCP Server Toolkit Status Check
# Quick status verification script

Write-Host "🔍 MCP Server Toolkit Status Check" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

# Check Node.js processes
$nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
Write-Host "📊 Node.js Processes: $($nodeProcesses.Count) running" -ForegroundColor Yellow

if ($nodeProcesses.Count -ge 10) {
    Write-Host "✅ Sufficient Node.js processes detected" -ForegroundColor Green
} else {
    Write-Host "⚠️  Expected 10+ processes, found $($nodeProcesses.Count)" -ForegroundColor Yellow
}

# Check ports
$port3000 = Test-NetConnection -ComputerName localhost -Port 3000 -WarningAction SilentlyContinue -InformationLevel Quiet
$port3001 = Test-NetConnection -ComputerName localhost -Port 3001 -WarningAction SilentlyContinue -InformationLevel Quiet

Write-Host "`n🌐 Port Status:" -ForegroundColor Cyan
if ($port3000.TcpTestSucceeded) {
    Write-Host "✅ Port 3000 (Web Interface): Active" -ForegroundColor Green
} else {
    Write-Host "❌ Port 3000 (Web Interface): Not responding" -ForegroundColor Red
}

if ($port3001.TcpTestSucceeded) {
    Write-Host "✅ Port 3001 (Dashboard): Active" -ForegroundColor Green
} else {
    Write-Host "❌ Port 3001 (Dashboard): Not responding" -ForegroundColor Red
}

# Check MCP toolkit directory
$mcpPath = "H:\Cursor\mcp_server_toolkit"
if (Test-Path $mcpPath) {
    Write-Host "`n📁 MCP Toolkit Directory: ✅ Found" -ForegroundColor Green
    $serverFiles = Get-ChildItem -Path $mcpPath -Name "*-mcp-server.js" | Measure-Object
    Write-Host "   Server Files: $($serverFiles.Count) found" -ForegroundColor Gray
} else {
    Write-Host "`n📁 MCP Toolkit Directory: ❌ Not found" -ForegroundColor Red
}

# Overall status
Write-Host "`n📈 Overall Status:" -ForegroundColor Cyan
$allGood = $nodeProcesses.Count -ge 10 -and $port3000.TcpTestSucceeded -and $port3001.TcpTestSucceeded

if ($allGood) {
    Write-Host "🎉 MCP Server Toolkit is fully operational!" -ForegroundColor Green
    Write-Host "`n🌐 Access URLs:" -ForegroundColor Yellow
    Write-Host "   Web Interface: http://localhost:3000" -ForegroundColor Gray
    Write-Host "   Dashboard: http://localhost:3001" -ForegroundColor Gray
} else {
    Write-Host "⚠️  Some issues detected. Check the details above." -ForegroundColor Yellow
    Write-Host "🔄 To restart: .\stop-mcp-servers.ps1 && .\start-mcp-servers.ps1" -ForegroundColor Gray
}

Write-Host "`n🔧 Quick Commands:" -ForegroundColor Cyan
Write-Host "   Status: .\check-mcp-status.ps1" -ForegroundColor Gray
Write-Host "   Start: .\start-mcp-servers.ps1" -ForegroundColor Gray
Write-Host "   Stop: .\stop-mcp-servers.ps1" -ForegroundColor Gray
Write-Host "   Auto-startup: .\setup-auto-startup.ps1" -ForegroundColor Gray 