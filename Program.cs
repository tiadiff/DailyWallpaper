using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DailyWallpaper;

static class Program
{
    private const int SPI_SETDESKWALLPAPER = 0x0014;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDWININICHANGE = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private static NotifyIcon? _notifyIcon;
    private static System.Threading.Timer? _timer;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Setup System Tray Icon
        _notifyIcon = new NotifyIcon()
        {
            Icon = new System.Drawing.Icon("icon.ico"),
            Text = "Daily Wallpaper",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        var exitMenuItem = new ToolStripMenuItem("Exit");
        exitMenuItem.Click += (s, e) => Application.Exit();
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Perform initial update asynchronously
        Task.Run(UpdateWallpaperAsync);

        // Schedule timer to trigger at next midnight
        ScheduleNextUpdate();

        Application.Run();

        // Cleanup
        _notifyIcon.Dispose();
        _timer?.Dispose();
    }

    private static void ScheduleNextUpdate()
    {
        var now = DateTime.Now;
        var tomorrow = now.Date.AddDays(1);
        var timeUntilMidnight = tomorrow - now;

        _timer = new System.Threading.Timer(
            e =>
            {
                Task.Run(UpdateWallpaperAsync);
                ScheduleNextUpdate();
            },
            null,
            timeUntilMidnight,
            Timeout.InfiniteTimeSpan
        );
    }

    private static async Task UpdateWallpaperAsync()
    {
        try
        {
            // 1. Fetch Bing Daily Image URL
            string? imageUrl = await GetBingDailyImageUrlAsync();
            if (string.IsNullOrEmpty(imageUrl)) return;

            // 2. Download Image
            string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string wallpapersFolder = Path.Combine(picturesFolder, "BingWallpapers");
            if (!Directory.Exists(wallpapersFolder))
            {
                Directory.CreateDirectory(wallpapersFolder);
            }
            
            // Use current date for filename to avoid duplicate downloads on the same day
            string filename = $"bing_{DateTime.Now:yyyy-MM-dd}.jpg";
            string localFilePath = Path.Combine(wallpapersFolder, filename);

            if (!File.Exists(localFilePath))
            {
                await DownloadImageAsync(imageUrl, localFilePath);
            }

            // 3. Set Wallpaper
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, localFilePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

            // 4. Set Registry for Auto Colorization
            EnableAutoColorization();
        }
        catch
        {
            // Background thread exception swallowing (silently fail in background)
        }
    }

    static async Task<string?> GetBingDailyImageUrlAsync()
    {
        string bingApiUrl = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-US";
        using HttpClient client = new HttpClient();
        
        string json = await client.GetStringAsync(bingApiUrl);
        using JsonDocument doc = JsonDocument.Parse(json);
        
        JsonElement root = doc.RootElement;
        if (root.TryGetProperty("images", out JsonElement images) && images.GetArrayLength() > 0)
        {
            JsonElement firstImage = images[0];
            
            // Try to get the UHD version using urlbase
            if (firstImage.TryGetProperty("urlbase", out JsonElement urlbaseElement))
            {
                string? urlbase = urlbaseElement.GetString();
                if (!string.IsNullOrEmpty(urlbase))
                {
                    string uhdUrl = urlbase + "_UHD.jpg";
                    if (uhdUrl.StartsWith("/")) return "https://www.bing.com" + uhdUrl;
                    return uhdUrl;
                }
            }

            // Fallback to the standard url
            if (firstImage.TryGetProperty("url", out JsonElement urlElement))
            {
                string? urlPath = urlElement.GetString();
                if (!string.IsNullOrEmpty(urlPath))
                {
                    if (urlPath.StartsWith("/")) return "https://www.bing.com" + urlPath;
                    return urlPath;
                }
            }
        }
        return null;
    }

    static async Task DownloadImageAsync(string url, string localPath)
    {
        using HttpClient client = new HttpClient();
        byte[] imageBytes = await client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(localPath, imageBytes);
    }

    static void EnableAutoColorization()
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true))
            {
                if (key != null) key.SetValue("AutoColorization", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\DWM", writable: true))
            {
                if (key != null) key.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
