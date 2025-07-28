# WindowsAppsManager Backup Issue Diagnostics
# This script tests backup functionality and reports detailed error information

Write-Host "=== WindowsAppsManager Backup Diagnostics ===" -ForegroundColor Cyan
Write-Host "Testing backup functionality and permissions..." -ForegroundColor Yellow

# Check if the application exists
$appPath = ".\bin\Any CPU\Release\net5.0-windows\WindowsAppsManager.exe"
if (-not (Test-Path $appPath)) {
    Write-Host "❌ ERROR: WindowsAppsManager.exe not found at: $appPath" -ForegroundColor Red
    Write-Host "Please run 'dotnet build --configuration Release' first." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Application found at: $appPath" -ForegroundColor Green

# Check backup directory permissions
$backupDir = [System.IO.Path]::Combine([Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments), "WindowsAppsManager", "Backups")
Write-Host "📁 Testing backup directory: $backupDir" -ForegroundColor Cyan

# Test backup directory creation
try {
    if (-not (Test-Path $backupDir)) {
        New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
        Write-Host "✅ Backup directory created successfully" -ForegroundColor Green
    } else {
        Write-Host "✅ Backup directory already exists" -ForegroundColor Green
    }
    
    # Test write permissions
    $testFile = Join-Path $backupDir "permission_test.txt"
    "Test" | Out-File -FilePath $testFile -Force
    Remove-Item $testFile -Force
    Write-Host "✅ Backup directory is writable" -ForegroundColor Green
} catch {
    Write-Host "❌ ERROR: Cannot access backup directory - $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Suggestion: Try running as Administrator" -ForegroundColor Yellow
}

# Test basic Windows app enumeration
Write-Host "🔍 Testing Windows app enumeration..." -ForegroundColor Cyan
try {
    $packages = Get-AppxPackage | Select-Object -First 5 Name, PackageFullName, InstallLocation
    Write-Host "✅ Found $($packages.Count) test packages:" -ForegroundColor Green
    foreach ($pkg in $packages) {
        Write-Host "   • $($pkg.Name)" -ForegroundColor Gray
        if ($pkg.InstallLocation -and (Test-Path $pkg.InstallLocation)) {
            Write-Host "     📁 Location: $($pkg.InstallLocation)" -ForegroundColor DarkGray
        } else {
            Write-Host "     ⚠️  Location: Not accessible or not found" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "❌ ERROR: Cannot enumerate Windows apps - $($_.Exception.Message)" -ForegroundColor Red
}

# Test if .NET 5.0 runtime is available
Write-Host "🔧 Testing .NET runtime..." -ForegroundColor Cyan
try {
    $dotnetVersion = & dotnet --version 2>$null
    if ($dotnetVersion) {
        Write-Host "✅ .NET runtime available: $dotnetVersion" -ForegroundColor Green
    } else {
        Write-Host "⚠️  .NET runtime check inconclusive" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Could not verify .NET runtime" -ForegroundColor Yellow
}

# Memory and disk space check
Write-Host "💾 Testing system resources..." -ForegroundColor Cyan
try {
    $disk = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DeviceID -eq "C:" }
    $freeSpaceGB = [math]::Round($disk.FreeSpace / 1GB, 2)
    Write-Host "✅ Free disk space: $freeSpaceGB GB" -ForegroundColor Green
    
    if ($freeSpaceGB -lt 1) {
        Write-Host "⚠️  WARNING: Low disk space may cause backup failures" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Could not check disk space" -ForegroundColor Yellow
}

Write-Host "`n🚀 Starting application to test backup..." -ForegroundColor Cyan
Write-Host "💡 In the application:" -ForegroundColor Yellow
Write-Host "   1. Select a small app (like Calculator)" -ForegroundColor White
Write-Host "   2. Try the backup function" -ForegroundColor White
Write-Host "   3. Check the console output for detailed error messages" -ForegroundColor White
Write-Host "   4. Close the application to see this script continue" -ForegroundColor White

# Launch the application and wait for it to close
try {
    $process = Start-Process -FilePath $appPath -PassThru -WindowStyle Normal
    Write-Host "✅ Application launched (PID: $($process.Id))" -ForegroundColor Green
    Write-Host "⏳ Waiting for application to close..." -ForegroundColor Yellow
    $process.WaitForExit()
    Write-Host "✅ Application closed" -ForegroundColor Green
} catch {
    Write-Host "❌ ERROR: Could not launch application - $($_.Exception.Message)" -ForegroundColor Red
}

# Check if any backup files were created
Write-Host "`n📊 Post-test analysis..." -ForegroundColor Cyan
try {
    if (Test-Path $backupDir) {
        $backupFiles = Get-ChildItem $backupDir -Recurse | Measure-Object
        Write-Host "📁 Backup directory contains $($backupFiles.Count) files/folders" -ForegroundColor Green
        
        $recentBackups = Get-ChildItem $backupDir -Directory | Where-Object { $_.CreationTime -gt (Get-Date).AddHours(-1) }
        if ($recentBackups) {
            Write-Host "✅ Recent backup folders found:" -ForegroundColor Green
            foreach ($backup in $recentBackups) {
                Write-Host "   • $($backup.Name) ($(($backup.CreationTime).ToString('yyyy-MM-dd HH:mm:ss')))" -ForegroundColor Gray
            }
        } else {
            Write-Host "⚠️  No recent backup folders found in the last hour" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️  Backup directory was not created" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ ERROR checking backup results: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Backup Diagnostics Complete ===" -ForegroundColor Cyan
Write-Host "💡 If backup issues persist:" -ForegroundColor Yellow
Write-Host "   1. Check Windows Event Viewer for .NET application errors" -ForegroundColor White
Write-Host "   2. Try running as Administrator" -ForegroundColor White
Write-Host "   3. Verify disk space and permissions" -ForegroundColor White
Write-Host "   4. Test with a simple app like Calculator first" -ForegroundColor White 