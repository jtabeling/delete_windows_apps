param(
    [switch]$DryRun = $false
)

Write-Host "🧪 TESTING ENHANCED ZUNE MUSIC DELETION" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$PackageName = "Microsoft.ZuneMusic_10.21102.11411.0_x64__8wekyb3d8bbwe"
$FolderPath = "C:\Program Files\WindowsApps\Microsoft.ZuneMusic_10.21102.11411.0_x64__8wekyb3d8bbwe"

Write-Host "📋 Target Package: $PackageName" -ForegroundColor White
Write-Host "📁 Target Folder: $FolderPath" -ForegroundColor White
Write-Host ""

# Pre-test verification
Write-Host "🔍 PRE-TEST VERIFICATION" -ForegroundColor Yellow
Write-Host "========================" -ForegroundColor Yellow

# Check if package exists in PowerShell
$registeredPackage = Get-AppxPackage -Name "*Microsoft.ZuneMusic*" | Where-Object {$_.PackageFullName -eq $PackageName}
if ($registeredPackage) {
    Write-Host "✅ Package found in PowerShell registry" -ForegroundColor Green
    Write-Host "   Status: $($registeredPackage.Status)" -ForegroundColor White
    Write-Host "   InstallLocation: $($registeredPackage.InstallLocation)" -ForegroundColor White
} else {
    Write-Host "❌ Package NOT found in PowerShell registry" -ForegroundColor Red
    Write-Host "   (May have been previously removed)" -ForegroundColor Yellow
}

# Check if folder exists
if (Test-Path $FolderPath) {
    $files = Get-ChildItem $FolderPath -Recurse -File -ErrorAction SilentlyContinue
    Write-Host "✅ Folder exists with $($files.Count) files" -ForegroundColor Green
    
    # Show folder size
    $totalSize = ($files | Measure-Object -Property Length -Sum).Sum
    $sizeInMB = [math]::Round($totalSize / 1MB, 2)
    Write-Host "   Total size: $sizeInMB MB" -ForegroundColor White
} else {
    Write-Host "❌ Folder does not exist" -ForegroundColor Red
    Write-Host "   (May have been previously deleted)" -ForegroundColor Yellow
}

Write-Host ""

# If dry run, just show what would happen
if ($DryRun) {
    Write-Host "🧪 DRY RUN MODE - Simulating deletion process..." -ForegroundColor Magenta
    Write-Host ""
    
    Write-Host "📋 ENHANCED DELETION PROCESS SIMULATION:" -ForegroundColor Cyan
    Write-Host "🚀 Starting deletion process for Zune Music"
    Write-Host "📋 Package: $PackageName"
    Write-Host "📁 Folder: $FolderPath"
    Write-Host ""
    Write-Host "🔍 Phase 1: Pre-deletion analysis..."
    Write-Host "📊 Analysis result: Package and folder both present"
    Write-Host "📁 Initial folder status: EXISTS"
    Write-Host ""
    Write-Host "🔄 Phase 2: Terminating related processes..."
    Write-Host "✅ No Zune/Music processes found running"
    Write-Host ""
    Write-Host "🔷 Phase 3: PowerShell-based deletion..."
    Write-Host "🔸 Executing: Remove-AppxPackage -Package '$PackageName' -Confirm:`$false -ErrorAction Stop"
    Write-Host "✅ PowerShell deletion reported success"
    Write-Host ""
    Write-Host "🔍 Phase 4: Post-deletion verification..."
    Write-Host "⚠️  PARTIAL SUCCESS: Package unregistered but folder persists"
    Write-Host "🔧 Initiating enhanced folder cleanup..."
    Write-Host ""
    Write-Host "📊 Found $($files.Count) files and subdirectories to clean up"
    Write-Host "🔄 Step 1: Terminating related processes..."
    Write-Host "🔐 Step 2: Taking ownership and fixing permissions..."
    Write-Host "🔸 Executing: takeown /f `"$FolderPath`" /r /d y"
    Write-Host "🔸 Executing: icacls `"$FolderPath`" /grant `"$env:USERNAME:F`" /T /C /Q"
    Write-Host "🔸 Executing: icacls `"$FolderPath`" /grant Everyone:F /T /C /Q"
    Write-Host "🗑️  Step 3: Attempting folder deletion..."
    Write-Host "🔸 Attempting standard folder deletion..."
    Write-Host "🔸 Attempting force deletion with CMD..."
    Write-Host "✅ CLEANUP SUCCESS: Folder removed after manual intervention"
    Write-Host ""
    Write-Host "🧪 DRY RUN COMPLETE - No actual changes made" -ForegroundColor Magenta
    return
}

# Real test execution
Write-Host "⚠️  REAL DELETION TEST" -ForegroundColor Red
Write-Host "=====================" -ForegroundColor Red
Write-Host "This will attempt to actually delete the Zune Music app!" -ForegroundColor Red
Write-Host ""

$response = Read-Host "Are you sure you want to proceed? (Type 'YES' to continue)"
if ($response -ne "YES") {
    Write-Host "❌ Test cancelled by user" -ForegroundColor Yellow
    return
}

Write-Host ""
Write-Host "🚀 EXECUTING REAL DELETION TEST..." -ForegroundColor Red

# Test the PowerShell deletion command that we know works
if ($registeredPackage) {
    Write-Host "🔷 Testing PowerShell removal..." -ForegroundColor Yellow
    
    try {
        Write-Host "🔸 Executing: Remove-AppxPackage -Package '$PackageName'" -ForegroundColor White
        Remove-AppxPackage -Package $PackageName -Confirm:$false -ErrorAction Stop
        Write-Host "✅ PowerShell removal completed" -ForegroundColor Green
    } catch {
        Write-Host "❌ PowerShell removal failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Wait for system to update
    Start-Sleep -Seconds 2
    
    # Check status after PowerShell removal
    Write-Host ""
    Write-Host "🔍 POST-POWERSHELL VERIFICATION:" -ForegroundColor Yellow
    
    $packageAfter = Get-AppxPackage -Name "*Microsoft.ZuneMusic*" | Where-Object {$_.PackageFullName -eq $PackageName}
    $folderAfter = Test-Path $FolderPath
    
    if (!$packageAfter -and !$folderAfter) {
        Write-Host "✅ COMPLETE SUCCESS: Package unregistered and folder removed" -ForegroundColor Green
    } elseif (!$packageAfter -and $folderAfter) {
        Write-Host "⚠️  PARTIAL SUCCESS: Package unregistered but folder persists" -ForegroundColor Yellow
        
        # This is where our enhanced cleanup would kick in
        Write-Host "🔧 Enhanced cleanup would now be triggered..." -ForegroundColor Cyan
        
        # Test manual cleanup (simplified version)
        Write-Host "🔐 Testing permission fixes..." -ForegroundColor Yellow
        
        try {
            # Try taking ownership
            Write-Host "🔸 Taking ownership..." -ForegroundColor White
            & takeown /f "$FolderPath" /r /d y 2>$null | Out-Null
            
            # Try granting permissions
            Write-Host "🔸 Granting permissions..." -ForegroundColor White
            & icacls "$FolderPath" /grant "${env:USERNAME}:F" /T /C /Q 2>$null | Out-Null
            
            # Try deletion
            Write-Host "🔸 Attempting folder deletion..." -ForegroundColor White
            Remove-Item -Path $FolderPath -Recurse -Force -ErrorAction Stop
            
            if (!(Test-Path $FolderPath)) {
                Write-Host "✅ ENHANCED CLEANUP SUCCESSFUL: Folder removed" -ForegroundColor Green
            } else {
                Write-Host "⚠️  Partial cleanup: Some files may remain" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "❌ Enhanced cleanup failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ PowerShell removal failed - both package and folder still exist" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  Package not registered - testing folder cleanup only..." -ForegroundColor Yellow
    
    if (Test-Path $FolderPath) {
        Write-Host "🔧 Testing direct folder cleanup..." -ForegroundColor Cyan
        
        try {
            # Test the enhanced permission and deletion logic
            Write-Host "🔸 Taking ownership..." -ForegroundColor White
            & takeown /f "$FolderPath" /r /d y 2>$null | Out-Null
            
            Write-Host "🔸 Granting permissions..." -ForegroundColor White
            & icacls "$FolderPath" /grant "${env:USERNAME}:F" /T /C /Q 2>$null | Out-Null
            
            Write-Host "🔸 Attempting folder deletion..." -ForegroundColor White
            Remove-Item -Path $FolderPath -Recurse -Force -ErrorAction Stop
            
            if (!(Test-Path $FolderPath)) {
                Write-Host "✅ FOLDER CLEANUP SUCCESSFUL" -ForegroundColor Green
            } else {
                Write-Host "⚠️  Partial cleanup: Some files may remain" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "❌ Folder cleanup failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "✅ No folder to clean up" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "🏁 TEST COMPLETE" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan

# Final verification
Write-Host "🔍 FINAL VERIFICATION:" -ForegroundColor Yellow
$finalPackage = Get-AppxPackage -Name "*Microsoft.ZuneMusic*" | Where-Object {$_.PackageFullName -eq $PackageName}
$finalFolder = Test-Path $FolderPath

Write-Host "   Package registered: $(if ($finalPackage) { 'YES' } else { 'NO' })" -ForegroundColor White
Write-Host "   Folder exists: $(if ($finalFolder) { 'YES' } else { 'NO' })" -ForegroundColor White

if (!$finalPackage -and !$finalFolder) {
    Write-Host "🎉 DELETION TEST SUCCESSFUL - Zune Music completely removed!" -ForegroundColor Green
} elseif (!$finalPackage -and $finalFolder) {
    Write-Host "⚠️  PARTIAL SUCCESS - Package removed but folder remains" -ForegroundColor Yellow
    Write-Host "   (This would be handled by the enhanced app logic)" -ForegroundColor Yellow
} else {
    Write-Host "❌ DELETION TEST FAILED - App may still be present" -ForegroundColor Red
}

Write-Host ""
Write-Host "✅ Enhanced Windows App Manager validation complete!" -ForegroundColor Green 