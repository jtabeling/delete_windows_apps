param(
    [Parameter(Mandatory=$true)]
    [string]$PackageName,
    
    [Parameter(Mandatory=$true)]
    [string]$FolderPath
)

Write-Host "🔍 POST-DELETION DIAGNOSTIC ANALYSIS" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Package: $PackageName" -ForegroundColor White
Write-Host "Folder:  $FolderPath" -ForegroundColor White
Write-Host ""

# Check 1: PowerShell Registration Status
Write-Host "🔍 Checking PowerShell Registration..." -ForegroundColor Yellow
try {
    $baseName = $PackageName.Split('_')[0]
    $registeredPackage = Get-AppxPackage -Name "*$baseName*" | Where-Object {$_.PackageFullName -eq $PackageName}
    
    if ($registeredPackage) {
        Write-Host "❌ Package STILL REGISTERED in PowerShell:" -ForegroundColor Red
        Write-Host "   Name: $($registeredPackage.Name)" -ForegroundColor White
        Write-Host "   Status: $($registeredPackage.Status)" -ForegroundColor White
        Write-Host "   InstallLocation: $($registeredPackage.InstallLocation)" -ForegroundColor White
    } else {
        Write-Host "✅ Package NOT found in PowerShell (deletion successful)" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ PowerShell check failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Check 2: Folder Existence and Contents
Write-Host "📁 Checking Folder Status..." -ForegroundColor Yellow

if (-not (Test-Path $FolderPath)) {
    Write-Host "✅ Folder does not exist (deletion successful)" -ForegroundColor Green
} else {
    Write-Host "❌ Folder STILL EXISTS: $FolderPath" -ForegroundColor Red
    
    try {
        $folderInfo = Get-Item $FolderPath
        $files = Get-ChildItem $FolderPath -Recurse -File -ErrorAction SilentlyContinue
        $subdirs = Get-ChildItem $FolderPath -Recurse -Directory -ErrorAction SilentlyContinue
        
        Write-Host "   📄 Files: $($files.Count)" -ForegroundColor White
        Write-Host "   📂 Subdirectories: $($subdirs.Count)" -ForegroundColor White
        Write-Host "   📅 Created: $($folderInfo.CreationTime)" -ForegroundColor White
        Write-Host "   📅 Modified: $($folderInfo.LastWriteTime)" -ForegroundColor White
        
        # Show largest files
        $largestFiles = $files | Sort-Object Length -Descending | Select-Object -First 5
        Write-Host "   📋 Largest files:" -ForegroundColor White
        foreach ($file in $largestFiles) {
            try {
                Write-Host "      $($file.Name) ($($file.Length.ToString('N0')) bytes)" -ForegroundColor White
            } catch {
                Write-Host "      $($file.Name) (size unknown - may be locked)" -ForegroundColor White
            }
        }
    } catch {
        Write-Host "   ⚠️  Cannot analyze folder contents: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
Write-Host ""

# Check 3: Running Processes
Write-Host "🔄 Checking Running Processes..." -ForegroundColor Yellow

try {
    $appName = $PackageName.Split('_')[0].Split('.')[-1]
    $allProcesses = Get-Process -ErrorAction SilentlyContinue
    $relatedProcesses = @()
    
    foreach ($process in $allProcesses) {
        try {
            # Check if process name contains app name
            if ($process.ProcessName -like "*$appName*") {
                $relatedProcesses += $process
                continue
            }
            
            # Check if process is running from the app folder
            if ($process.Path -and $process.Path.StartsWith($FolderPath, [System.StringComparison]::OrdinalIgnoreCase)) {
                $relatedProcesses += $process
            }
        } catch {
            # Process access denied - normal for system processes
        }
    }
    
    if ($relatedProcesses.Count -gt 0) {
        Write-Host "❌ Found $($relatedProcesses.Count) related process(es) still running:" -ForegroundColor Red
        foreach ($process in $relatedProcesses) {
            try {
                Write-Host "   🔄 PID $($process.Id): $($process.ProcessName) ($($process.Path))" -ForegroundColor White
            } catch {
                Write-Host "   🔄 PID $($process.Id): $($process.ProcessName) (path unknown)" -ForegroundColor White
            }
        }
    } else {
        Write-Host "✅ No related processes found running" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Process check failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Check 4: File Handles (using handle.exe if available)
Write-Host "🔒 Checking File Handles..." -ForegroundColor Yellow

try {
    $handlePath = Get-Command "handle.exe" -ErrorAction SilentlyContinue
    if ($handlePath) {
        $handleOutput = & handle.exe "$FolderPath" 2>$null
        if ($handleOutput -and -not ($handleOutput -like "*No matching handles*")) {
            Write-Host "❌ File handles detected:" -ForegroundColor Red
            Write-Host $handleOutput -ForegroundColor White
        } else {
            Write-Host "✅ No file handles detected" -ForegroundColor Green
        }
    } else {
        Write-Host "⚠️  Handle.exe not found - install Sysinternals tools for better handle detection" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Handle check failed: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# Check 5: Permissions Analysis
Write-Host "🔑 Checking Permissions..." -ForegroundColor Yellow

if (-not (Test-Path $FolderPath)) {
    Write-Host "✅ Folder doesn't exist" -ForegroundColor Green
} else {
    try {
        $acl = Get-Acl $FolderPath -ErrorAction SilentlyContinue
        if ($acl) {
            Write-Host "📋 Current permissions:" -ForegroundColor White
            foreach ($access in $acl.Access) {
                Write-Host "   $($access.IdentityReference): $($access.FileSystemRights) ($($access.AccessControlType))" -ForegroundColor White
            }
        } else {
            Write-Host "❌ Cannot read permissions" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Permission check failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""

# Check 6: Windows Store Cache
Write-Host "🏪 Checking Windows Store Cache..." -ForegroundColor Yellow

try {
    $storePackages = Get-AppxPackage -Name "*$($PackageName.Split('_')[0])*" -AllUsers -ErrorAction SilentlyContinue
    if ($storePackages) {
        Write-Host "❌ Package still in Windows Store cache:" -ForegroundColor Red
        foreach ($pkg in $storePackages) {
            Write-Host "   Name: $($pkg.Name)" -ForegroundColor White
            Write-Host "   Status: $($pkg.Status)" -ForegroundColor White
            if ($pkg.PackageUserInformation) {
                Write-Host "   User Info: $($pkg.PackageUserInformation)" -ForegroundColor White
            }
        }
    } else {
        Write-Host "✅ Package not found in Windows Store cache" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Store cache check failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Check 7: Registry Entries
Write-Host "📊 Checking Registry Entries..." -ForegroundColor Yellow

try {
    $appName = $PackageName.Split('_')[0]
    $registryPaths = @(
        "HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages\$PackageName",
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications\$PackageName",
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched\$appName"
    )
    
    $foundRegistryEntries = 0
    foreach ($regPath in $registryPaths) {
        try {
            if (Test-Path $regPath) {
                Write-Host "   ❌ Found: $regPath" -ForegroundColor Red
                $foundRegistryEntries++
            } else {
                Write-Host "   ✅ Not found: $regPath" -ForegroundColor Green
            }
        } catch {
            Write-Host "   ⚠️  Cannot check: $regPath" -ForegroundColor Yellow
        }
    }
    
    if ($foundRegistryEntries -eq 0) {
        Write-Host "✅ No registry entries found" -ForegroundColor Green
    } else {
        Write-Host "❌ Found $foundRegistryEntries registry entries" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Registry check failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Recommendations
Write-Host "📋 RECOMMENDED ACTIONS:" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan

if (Test-Path $FolderPath) {
    Write-Host "1. Terminate any running processes related to the app" -ForegroundColor White
    Write-Host "2. Take ownership of the folder: takeown /f `"$FolderPath`" /r /d y" -ForegroundColor White
    Write-Host "3. Grant full permissions: icacls `"$FolderPath`" /grant Everyone:F /T /C" -ForegroundColor White
    Write-Host "4. Try manual folder deletion: rmdir /s /q `"$FolderPath`"" -ForegroundColor White
    Write-Host "5. If folder still persists, restart Windows and try again" -ForegroundColor White
} else {
    Write-Host "✅ No folder cleanup needed - deletion appears complete" -ForegroundColor Green
}

Write-Host ""
Write-Host "✅ Diagnostic complete!" -ForegroundColor Green 