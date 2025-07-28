# WindowsAppsManager - Deletion Log Checker
# This script helps you locate and view deletion operation logs

Write-Host "=== WindowsAppsManager Deletion Log Checker ===" -ForegroundColor Cyan
Write-Host ""

$logPath = "$env:USERPROFILE\Documents\WindowsAppsManager\Logs\deletion.log"
Write-Host "Log file location: $logPath" -ForegroundColor Yellow

if (Test-Path $logPath) {
    $logInfo = Get-Item $logPath
    Write-Host "✅ Log file exists!" -ForegroundColor Green
    Write-Host "   Size: $($logInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "   Last modified: $($logInfo.LastWriteTime)" -ForegroundColor Gray
    Write-Host ""
    
    # Show last 20 lines of the log
    Write-Host "=== LAST 20 LOG ENTRIES ===" -ForegroundColor Cyan
    Get-Content $logPath | Select-Object -Last 20 | ForEach-Object {
        if ($_ -match "CRITICAL|ERROR|FAILURE") {
            Write-Host $_ -ForegroundColor Red
        }
        elseif ($_ -match "SUCCESS|COMPLETE") {
            Write-Host $_ -ForegroundColor Green
        }
        elseif ($_ -match "WARNING|PARTIAL") {
            Write-Host $_ -ForegroundColor Yellow
        }
        else {
            Write-Host $_ -ForegroundColor White
        }
    }
    
    Write-Host ""
    Write-Host "To view the full log: notepad `"$logPath`"" -ForegroundColor Cyan
} else {
    Write-Host "❌ Log file not found" -ForegroundColor Red
    Write-Host "   The log file will be created when you perform your first app deletion." -ForegroundColor Gray
    Write-Host "   Make sure WindowsAppsManager has write permissions to Documents folder." -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== LOG ANALYSIS TIPS ===" -ForegroundColor Cyan
Write-Host "• Look for 'CRITICAL FAILURE' entries - these indicate PowerShell deletion failures"
Write-Host "• 'INTEGRITY PROTECTION ACTIVATED' shows when dangerous manual deletion was prevented"
Write-Host "• 'COMPLETE SUCCESS' indicates proper app unregistration and folder removal"
Write-Host "• Each deletion operation starts with '=== DELETION START' and ends with '=== DELETION END'"
Write-Host ""

# Check for common issues
if (Test-Path $logPath) {
    $recentEntries = Get-Content $logPath | Select-Object -Last 100
    
    $criticalFailures = $recentEntries | Where-Object { $_ -match "CRITICAL FAILURE" }
    $integrityProtections = $recentEntries | Where-Object { $_ -match "INTEGRITY PROTECTION" }
    $powershellErrors = $recentEntries | Where-Object { $_ -match "PowerShell.*failed|Remove-AppxPackage.*failed" }
    
    if ($criticalFailures.Count -gt 0) {
        Write-Host "⚠️  ANALYSIS: Found $($criticalFailures.Count) critical failure(s) in recent logs" -ForegroundColor Yellow
        Write-Host "   This indicates PowerShell Remove-AppxPackage commands are failing" -ForegroundColor Gray
    }
    
    if ($integrityProtections.Count -gt 0) {
        Write-Host "🛡️  ANALYSIS: Found $($integrityProtections.Count) integrity protection activation(s)" -ForegroundColor Green
        Write-Host "   This shows the app prevented dangerous manual deletions that could cause integrity issues" -ForegroundColor Gray
    }
    
    if ($powershellErrors.Count -gt 0) {
        Write-Host "🔷 ANALYSIS: Found $($powershellErrors.Count) PowerShell error(s)" -ForegroundColor Yellow
        Write-Host "   Review these entries to understand why Remove-AppxPackage is failing" -ForegroundColor Gray
    }
} 