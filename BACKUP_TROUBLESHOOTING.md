# WindowsAppsManager Backup Troubleshooting Guide

## 🚨 Common Backup Failure Scenarios

### 1. **Permission Issues**
**Symptoms:** Access denied errors, folder creation failures
**Solutions:**
- Run WindowsAppsManager as Administrator
- Check if backup directory is accessible: `%USERPROFILE%\Documents\WindowsAppsManager\Backups`
- Verify write permissions to Documents folder

### 2. **App Folder Access Issues**  
**Symptoms:** "Directory not found" or "Access denied" when reading app files
**Solutions:**
- Some apps have restricted folder access (Windows Store protection)
- Try backing up non-Microsoft apps first (like games or third-party apps)
- Avoid system apps like "Windows Security" or "Microsoft Store"

### 3. **Disk Space Issues**
**Symptoms:** Backup starts but fails partway through
**Solutions:**
- Check available disk space (need at least 1GB free)
- Large apps may require several GB of space
- Choose smaller apps for testing first

### 4. **Path Length Issues**
**Symptoms:** Errors with files that have very long names
**Solutions:**
- This is handled automatically in the code (path length checks)
- If still occurs, the app should skip problematic files and continue

### 5. **Memory Issues**
**Symptoms:** App freezes or crashes during backup
**Solutions:**
- Close other applications to free memory
- Try backing up one app at a time instead of multiple
- Restart the application if it becomes unresponsive

## 🔧 Diagnostic Steps

### Step 1: Basic Checks
```powershell
# Check backup directory
$backupDir = "$env:USERPROFILE\Documents\WindowsAppsManager\Backups"
Test-Path $backupDir
# Should return True - if False, permission issue

# Check write access
"test" | Out-File "$backupDir\test.txt" -Force
Remove-Item "$backupDir\test.txt" -Force
# If this fails, permission issue
```

### Step 2: Test with Simple Apps
Try backing up in this order (easiest to hardest):
1. **Calculator** (Microsoft.WindowsCalculator)
2. **Paint** (Microsoft.MSPaint) 
3. **Notepad** (Microsoft.WindowsNotepad)
4. **Clock** (Microsoft.WindowsAlarms)
5. Third-party apps (games, utilities)

### Step 3: Check App Folder Access
```powershell
# Example: Check if you can access Calculator folder
$calcPath = Get-AppxPackage -Name "*Calculator*" | Select-Object -ExpandProperty InstallLocation
Test-Path $calcPath
Get-ChildItem $calcPath | Measure-Object
# Should show files - if access denied, that's the issue
```

## 🛠️ Manual Fixes

### Fix 1: Reset Backup Directory
```powershell
$backupDir = "$env:USERPROFILE\Documents\WindowsAppsManager\Backups"
Remove-Item $backupDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item $backupDir -ItemType Directory -Force
```

### Fix 2: Run as Administrator
- Right-click WindowsAppsManager.exe
- Select "Run as administrator"
- Try backup again

### Fix 3: Check Windows Defender
- Windows Defender may block access to some app folders  
- Add WindowsAppsManager to Defender exclusions
- Or temporarily disable real-time protection for testing

## 🐛 Error Message Translations

| Error Message | Likely Cause | Solution |
|---------------|--------------|----------|
| "Access denied" | Permissions | Run as admin or check folder permissions |
| "Directory not found" | App folder inaccessible | Try different app or check if app exists |
| "Disk space" | Low storage | Free up disk space |
| "Path too long" | Windows path limit | Should be handled automatically |
| "Memory" | Low RAM | Close other apps, restart WindowsAppsManager |

## 📋 Testing Checklist

**Before Backup:**
- [ ] WindowsAppsManager running as Administrator
- [ ] At least 1GB free disk space
- [ ] Backup directory exists and is writable
- [ ] Selected app is not a protected system component
- [ ] No other backup operations running

**During Backup:**
- [ ] Progress dialog shows detailed status messages
- [ ] No error messages in red text
- [ ] File count increasing steadily
- [ ] No "Access denied" or "Directory not found" errors

**After Backup:**
- [ ] New folder appears in backup directory
- [ ] Folder contains "AppFiles" subdirectory with files
- [ ] Backup metadata file exists
- [ ] No error messages in final status

## 🚀 Quick Test Commands

Run this PowerShell script to test backup environment:
```powershell
Write-Host "=== Backup Environment Test ===" -ForegroundColor Cyan

# Test 1: Backup directory
$backupDir = "$env:USERPROFILE\Documents\WindowsAppsManager\Backups"
if (Test-Path $backupDir) {
    Write-Host "✅ Backup directory exists" -ForegroundColor Green
} else {
    Write-Host "❌ Backup directory missing" -ForegroundColor Red
    New-Item $backupDir -ItemType Directory -Force
    Write-Host "✅ Created backup directory" -ForegroundColor Green
}

# Test 2: Write permissions
try {
    "test" | Out-File "$backupDir\test.txt" -Force
    Remove-Item "$backupDir\test.txt" -Force
    Write-Host "✅ Backup directory is writable" -ForegroundColor Green
} catch {
    Write-Host "❌ Cannot write to backup directory" -ForegroundColor Red
}

# Test 3: Find testable apps
$testApps = Get-AppxPackage | Where-Object { 
    $_.Name -match "(Calculator|Paint|Notepad|Clock)" -and $_.InstallLocation 
} | Select-Object Name, InstallLocation

if ($testApps) {
    Write-Host "✅ Found test apps:" -ForegroundColor Green
    $testApps | ForEach-Object { Write-Host "   • $($_.Name)" -ForegroundColor Gray }
} else {
    Write-Host "⚠️  No ideal test apps found" -ForegroundColor Yellow
}

Write-Host "=== Test Complete ===" -ForegroundColor Cyan
```

## 🎯 Next Steps

1. **Run the Quick Test** above to verify environment
2. **Try Administrator mode** if not already running  
3. **Start with Calculator** app for first backup test
4. **Report specific error messages** you see in the progress dialog
5. **Check Windows Event Viewer** for .NET application errors if crashes occur

The enhanced backup system should handle most issues automatically, but these steps will help identify any remaining problems. 