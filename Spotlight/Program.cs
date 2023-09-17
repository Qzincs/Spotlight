using Microsoft.Win32;
using Newtonsoft.Json;
using Spotlight;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

class Program
{
    static async Task Main(string[] args)
    {
        bool isRetry = true;
        while (isRetry)
        {
            Wallpaper wallpaper = new Wallpaper();
            // 获取当前壁纸的路径
            string currentWallpaperPath = GetCurrentWallpaperPath();
            
            // 下载或检查是否已经下载了今天的壁纸
            string todayWallpaperPath = await DownloadTodayWallpaper(wallpaper);

            // 如果下载成功，则设置为桌面壁纸
            if (todayWallpaperPath != null)
            {
                // 如果当前壁纸不存在或与今天的壁纸不同，则设置为今天的壁纸
                if (currentWallpaperPath == null || currentWallpaperPath != todayWallpaperPath)
                {
                    SetWallpaper(todayWallpaperPath);
                    Console.WriteLine($"壁纸设置成功！壁纸文件路径: {todayWallpaperPath}");
                    ShowWallpaperInfo();
                }
                else
                {
                    Console.WriteLine("今天已经设置过壁纸啦！壁纸文件路径：" + todayWallpaperPath);
                    ShowWallpaperInfo();
                }
                isRetry = false;
            }
            // 如果下载失败，并且之前已经下载过今天的壁纸，则使用之前下载的壁纸
            else if (currentWallpaperPath != null)
            {
                SetWallpaper(currentWallpaperPath);
                Console.Write("按Q键退出，按其他键重试：");
                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    Console.WriteLine("\n今日壁纸获取失败，不更新壁纸。当前壁纸文件路径：" + currentWallpaperPath);
                    return;
                }
                Console.WriteLine();
            }   
        }
        
        Console.Write("按任意键结束。");
        Console.ReadKey();
    }

    static void ShowWallpaperInfo()
    {
        string wallpaperDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SpotlightWallpapers");
        string wallpaperMetadataPath = Path.Combine(wallpaperDirectory, $"{DateTime.Now:yyyyMMdd}.txt");
        string[] info = File.ReadAllLines(wallpaperMetadataPath);
        Console.WriteLine($"今天的壁纸是{info[0]}\n{info[2]}\n{info[3]}");
    }

    // 获取当前壁纸的路径
    static string GetCurrentWallpaperPath()
    {
        // 打开注册表项 HKEY_CURRENT_USER\Control Panel\Desktop
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false);
        // 获取当前壁纸的路径
        string wallpaperPath = key.GetValue("Wallpaper") as string;
        return wallpaperPath;
    }

    // 下载或检查是否已经下载了今天的壁纸
    static async Task<string> DownloadTodayWallpaper(Wallpaper wallpaper)
    {
        // 构造今天壁纸的文件名（格式为 yyyymmdd.jpg）
        string todayWallpaperFileName = $"{DateTime.Now:yyyyMMdd}.jpg";
        string wallpaperMetadataFileName = $"{DateTime.Now:yyyyMMdd}.txt";

        // 构造壁纸保存的目录（位于用户图片文件夹下的 SpotlightWallpapers 文件夹中）
        string wallpaperDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SpotlightWallpapers");
        // 如果目录不存在，则创建目录
        if (!Directory.Exists(wallpaperDirectory))
        {
            Directory.CreateDirectory(wallpaperDirectory);
        }

        // 构造今天壁纸的保存路径
        string todayWallpaperPath = Path.Combine(wallpaperDirectory, todayWallpaperFileName);
        string wallpaperMetadataPath = Path.Combine(wallpaperDirectory, wallpaperMetadataFileName);

        // 如果已经下载了今天的壁纸，则直接返回该壁纸的路径
        if (File.Exists(todayWallpaperPath))
        {
            return todayWallpaperPath;
        }

        try
        {
            // 创建一个 HttpClient 实例
            using (var httpClient = new HttpClient())
            {
                bool isDuplicate = true;
                while (isDuplicate)
                {
                     string apiUrl = "https://arc.msn.com/v3/Delivery/Placement?pid=209567&fmt=json&cdm=1&pl=zh-CN&lc=zh-CN&ctry=CN";


                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        // 壁纸信息
                        dynamic responseData = JsonConvert.DeserializeObject(jsonString);
                        string imageJsonString = responseData.batchrsp.items[1].item;
                        imageJsonString = imageJsonString.Replace("\\\"", "\"");
                        dynamic image = JsonConvert.DeserializeObject(imageJsonString);
                        image = image.ad;


                        wallpaper.title = image.title_text.tx;
                        wallpaper.copyright = image.copyright_text.tx;
                        wallpaper.landscapeUrl = image.image_fullscreen_001_landscape.u;
                        wallpaper.landscapeSha256 = BitConverter.ToString(Convert.FromBase64String(image.image_fullscreen_001_landscape.sha256.ToString())).Replace("-", "");
                        wallpaper.portraitUrl = image.image_fullscreen_001_portrait.u;
                        wallpaper.portraitSha256 = BitConverter.ToString(Convert.FromBase64String(image.image_fullscreen_001_portrait.sha256.ToString())).Replace("-", "");
                    }
                    
                    // 检查壁纸是否重复
                    if (IsImageDuplicate(wallpaperDirectory, wallpaper.landscapeSha256))
                    {
                        continue;
                    }
                    isDuplicate = false;

                    // 构造元信息
                    string metadata = $"{wallpaper.title}\n{wallpaper.copyright}\n横向壁纸地址: {wallpaper.landscapeUrl}\n竖向壁纸地址: {wallpaper.portraitUrl}";

                    // 下载壁纸图片
                    HttpResponseMessage imageResponse = await httpClient.GetAsync(wallpaper.landscapeUrl);
                    byte[] imageData = await imageResponse.Content.ReadAsByteArrayAsync();

                    // 保存元信息
                    File.WriteAllText(wallpaperMetadataPath, metadata);
                    // 将壁纸保存到本地
                    File.WriteAllBytes(todayWallpaperPath, imageData);

                    // 返回壁纸的路径
                    return todayWallpaperPath;
                }
            }
        }
        catch (Exception ex)
        {
            // 打印异常信息
            Console.WriteLine(":( 获取壁纸时出现了以下异常：\n"+ex.Message);
        }

        // 下载失败，返回 null
        return null;
    }

    // 设置桌面壁纸
    static void SetWallpaper(string wallpaperPath)
    {
        // 判断系统是否为 Windows，如果不是，则直接返回
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // 打开注册表项 HKEY_CURRENT_USER\Control Panel\Desktop
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

        // 将 WallpaperStyle 和 TileWallpaper 的值分别设为 10 和 0
        // WallpaperStyle 设为 10 表示桌面壁纸不平铺，而是按比例拉伸
        // TileWallpaper 设为 0 表示不平铺壁纸
        key.SetValue("WallpaperStyle", "10");
        key.SetValue("TileWallpaper", "0");

        // 调用 SystemParametersInfo 函数设置桌面壁纸
        // SPI_SETDESKWALLPAPER 表示设置桌面壁纸
        // 0 表示更新所有桌面
        // wallpaperPath 表示壁纸的路径
        bool result = SystemParametersInfo(20, 0, wallpaperPath, 0x01 | 0x02);
        if (!result)
        {
            throw new Exception("Failed to set wallpaper.");
        }
    }

    // 检查壁纸是否重复
    static bool IsImageDuplicate(string path, string sha256)
    {
        string[] images = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);

        foreach (string image in images)
        {
            byte[] imageData = File.ReadAllBytes(image);
            string imageSHA256 = GetSHA256(imageData);

            if (imageSHA256.Equals(sha256, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // 获取文件SHA256
    static string GetSHA256(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }
    }

    // 调用 Windows API 的 SystemParametersInfo 函数
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}

