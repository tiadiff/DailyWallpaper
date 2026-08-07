# Daily Wallpaper Changer

A lightweight C# System Tray application that automatically changes your Windows desktop wallpaper every day using high-quality images from Bing. It runs silently in the background and automatically refreshes your wallpaper exactly at midnight! Additionally, it adjusts your Windows system accent color to match the new wallpaper, giving you a fresh and seamless desktop experience every day.

## Features
- **Daily Updates at Midnight**: The app stays alive in the system tray and fetches the new Bing Image of the Day automatically as soon as the clock strikes midnight.
- **Auto Colorization**: Forces Windows to extract the accent color from the newly set background and applies it to your taskbar, Start menu, and windows.
- **Local Archive**: Saves all downloaded wallpapers to your `Pictures\BingWallpapers` folder so you can keep them.
- **System Tray Integration**: Runs invisibly in the background with a minimal footprint. You can access the application menu by right-clicking its icon in the bottom right corner of your screen.

## Requirements
- Windows 10 or Windows 11
- .NET 8.0 SDK (to build and run)

## Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/DailyWallpaper.git
   cd DailyWallpaper
   ```

2. **Publish the executable:**
   Publish the app to a standalone executable so you don't need to run it via the .NET CLI.
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained false -o .\Publish
   ```

3. **Install the Startup Shortcut:**
   To make the app run automatically every time you log into Windows, execute the provided PowerShell script:
   ```powershell
   & ".\InstallTask.ps1"
   ```
   *Note: This script simply creates a shortcut to the `.exe` in your Windows `Startup` folder (`shell:startup`). No administrator privileges are required.*

## How to Uninstall
If you want to stop the daily updates:
1. Press `Win + R`, type `shell:startup`, and press Enter.
2. Delete the `DailyWallpaperChanger` shortcut.
3. Delete the `DailyWallpaper` project folder.

## License
MIT License
