# Fix Windows Defender Controlled Folder Access for WindowsAppsManager
# This script helps resolve "protected folder access blocked" errors

Write-Host "=== Windows Defender Controlled Folder Access Fix ===" -ForegroundColor Cyan
Write-Host "Fixing 'protected folder access blocked' error for backup..." -ForegroundColor Yellow

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "💡 Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "✅ Running as Administrator" -ForegroundColor Green

# Get WindowsAppsManager executable path
$appPath = Join-Path $PSScriptRoot "bin\Any CPU\Release\net5.0-windows\WindowsAppsManager.exe"
$fullAppPath = Resolve-Path $appPath -ErrorAction SilentlyContinue

if (-not $fullAppPath) {
    Write-Host "❌ ERROR: WindowsAppsManager.exe not found at: $appPath" -ForegroundColor Red
    Write-Host "💡 Please run this script from the project directory" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "✅ Found WindowsAppsManager at: $fullAppPath" -ForegroundColor Green

# Check current Controlled Folder Access status
Write-Host "`n🔍 Checking Windows Defender Controlled Folder Access status..." -ForegroundColor Cyan
try {
    $cfaStatus = Get-MpPreference | Select-Object -ExpandProperty EnableControlledFolderAccess
    switch ($cfaStatus) {
        0 { Write-Host "✅ Controlled Folder Access is DISABLED" -ForegroundColor Green }
        1 { Write-Host "⚠️  Controlled Folder Access is ENABLED" -ForegroundColor Yellow }
        2 { Write-Host "🔍 Controlled Folder Access is in AUDIT mode" -ForegroundColor Blue }
        default { Write-Host "❓ Controlled Folder Access status unknown: $cfaStatus" -ForegroundColor Gray }
    }
} catch {
    Write-Host "⚠️  Could not check Controlled Folder Access status" -ForegroundColor Yellow
}

# Show current allowed applications
Write-Host "`n📋 Current allowed applications for Controlled Folder Access:" -ForegroundColor Cyan
try {
    $allowedApps = Get-MpPreference | Select-Object -ExpandProperty ControlledFolderAccessAllowedApplications
    if ($allowedApps) {
        foreach ($app in $allowedApps) {
            Write-Host "   • $app" -ForegroundColor Gray
        }
    } else {
        Write-Host "   (No applications currently allowed)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   Could not retrieve allowed applications list" -ForegroundColor Yellow
}

# Offer solutions
Write-Host "`n🛠️  SOLUTION OPTIONS:" -ForegroundColor Cyan
Write-Host "1. Add WindowsAppsManager to allowed applications (RECOMMENDED)" -ForegroundColor White
Write-Host "2. Temporarily disable Controlled Folder Access" -ForegroundColor White
Write-Host "3. Exit and configure manually" -ForegroundColor White

$choice = Read-Host "`nEnter your choice (1-3)"

switch ($choice) {
    "1" {
        Write-Host "`n🔧 Adding WindowsAppsManager to allowed applications..." -ForegroundColor Yellow
        try {
            Add-MpPreference -ControlledFolderAccessAllowedApplications $fullAppPath.Path
            Write-Host "✅ SUCCESS: WindowsAppsManager added to allowed applications" -ForegroundColor Green
            Write-Host "💡 You can now run backup operations safely" -ForegroundColor Cyan
        } catch {
            Write-Host "❌ ERROR: Failed to add application: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "💡 Try running Windows Security manually and adding the app" -ForegroundColor Yellow
        }
    }
    "2" {
        Write-Host "`n⚠️  WARNING: This will temporarily disable folder protection" -ForegroundColor Yellow
        $confirm = Read-Host "Are you sure? (y/N)"
        if ($confirm -eq 'y' -or $confirm -eq 'Y') {
            try {
                Set-MpPreference -EnableControlledFolderAccess Disabled
                Write-Host "✅ Controlled Folder Access temporarily disabled" -ForegroundColor Green
                Write-Host "⚠️  REMEMBER: Re-enable it after backup testing!" -ForegroundColor Yellow
                Write-Host "   Command to re-enable: Set-MpPreference -EnableControlledFolderAccess Enabled" -ForegroundColor Gray
            } catch {
                Write-Host "❌ ERROR: Failed to disable: $($_.Exception.Message)" -ForegroundColor Red
            }
        } else {
            Write-Host "Cancelled - no changes made" -ForegroundColor Gray
        }
    }
    "3" {
        Write-Host "`n📱 Manual Configuration Steps:" -ForegroundColor Cyan
        Write-Host "1. Open Windows Security (Windows Defender)" -ForegroundColor White
        Write-Host "2. Go to Virus & threat protection" -ForegroundColor White
        Write-Host "3. Click 'Manage ransomware protection'" -ForegroundColor White
        Write-Host "4. Under 'Controlled folder access', click 'Allow an app through Controlled folder access'" -ForegroundColor White
        Write-Host "5. Click '+ Add an allowed app' → 'Browse for an app'" -ForegroundColor White
        Write-Host "6. Navigate to and select: $($fullAppPath.Path)" -ForegroundColor White
        Write-Host "7. Click 'Add' to allow WindowsAppsManager" -ForegroundColor White
    }
    default {
        Write-Host "Invalid choice. Exiting..." -ForegroundColor Red
    }
}

Write-Host "`n🧪 Testing backup directory access..." -ForegroundColor Cyan
$backupDir = "$env:USERPROFILE\Documents\WindowsAppsManager\Backups"

try {
    # Test if we can create a test file
    $testFile = Join-Path $backupDir "access_test_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    "Access test successful" | Out-File $testFile -Force
    Remove-Item $testFile -Force
    Write-Host "✅ Backup directory access test PASSED" -ForegroundColor Green
    Write-Host "💡 WindowsAppsManager backup should now work" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Backup directory access test FAILED" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "💡 You may need to restart WindowsAppsManager after making changes" -ForegroundColor Cyan
}

Write-Host "`n=== Fix Complete ===" -ForegroundColor Cyan
Write-Host "🚀 Try running WindowsAppsManager backup again" -ForegroundColor Green
Read-Host "Press Enter to exit" 