$exePath = "C:\Users\matti\.gemini\antigravity-ide\scratch\DailyWallpaper\Publish\DailyWallpaper.exe"

if (-not (Test-Path $exePath)) {
    Write-Error "The executable was not found at $exePath. Make sure the program has been compiled correctly."
    exit 1
}

$startupFolder = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupFolder "DailyWallpaperChanger.lnk"

$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = $exePath
$Shortcut.Description = "Changes the desktop wallpaper daily by downloading the image of the day from Bing."
$Shortcut.WorkingDirectory = "C:\Users\matti\.gemini\antigravity-ide\scratch\DailyWallpaper\Publish"
$Shortcut.Save()

Write-Host "Shortcut successfully created in the Startup folder! The wallpaper will be updated on every boot." -ForegroundColor Green
