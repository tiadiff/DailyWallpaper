using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace DailyWallpaper;

class Program
{
    private const int SPI_SETDESKWALLPAPER = 0x0014;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDWININICHANGE = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Daily Wallpaper Changer...");

        try
        {
            // 1. Fetch Bing Daily Image URL
            string? imageUrl = await GetBingDailyImageUrlAsync();
            if (string.IsNullOrEmpty(imageUrl))
            {
                Console.WriteLine("Failed to get Bing image URL.");
                return;
            }
            Console.WriteLine($"Found image URL: {imageUrl}");

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
                Console.WriteLine($"Downloading image to {localFilePath}...");
                await DownloadImageAsync(imageUrl, localFilePath);
            }
            else
            {
                Console.WriteLine($"Image already exists at {localFilePath}.");
            }

            // 3. Set Wallpaper
            Console.WriteLine("Setting desktop wallpaper...");
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, localFilePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

            // 4. Set Registry for Auto Colorization
            Console.WriteLine("Updating registry for Auto Colorization...");
            EnableAutoColorization();
            
            Console.WriteLine("Done!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
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
            if (firstImage.TryGetProperty("url", out JsonElement urlElement))
            {
                string? urlPath = urlElement.GetString();
                if (!string.IsNullOrEmpty(urlPath))
                {
                    if (urlPath.StartsWith("/"))
                    {
                        // Some endpoints return 1920x1080. We can try to get UHD by replacing it, but 1920x1080 is safe.
                        return "https://www.bing.com" + urlPath;
                    }
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
                if (key != null)
                {
                    key.SetValue("AutoColorization", 1, RegistryValueKind.DWord);
                    Console.WriteLine("AutoColorization enabled.");
                }
            }

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\DWM", writable: true))
            {
                if (key != null)
                {
                    // Also enable color on taskbar/start menu (optional, but usually desired for full theme effect)
                    key.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);
                    Console.WriteLine("ColorPrevalence enabled.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update registry: {ex.Message}");
        }
    }
}
