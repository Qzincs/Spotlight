using System.Runtime.InteropServices;
using Microsoft.Win32;

class Program
{
    static async Task Main(string[] args)
    {
        bool isRetry = true;
        while (isRetry)
        {
            // 获取当前壁纸的路径
            string currentWallpaperPath = GetCurrentWallpaperPath();

            // 下载或检查是否已经下载了今天的壁纸
            string todayWallpaperPath = await DownloadTodayWallpaper();

            // 如果下载成功，则设置为桌面壁纸
            if (todayWallpaperPath != null)
            {
                // 如果当前壁纸不存在或与今天的壁纸不同，则设置为今天的壁纸
                if (currentWallpaperPath == null || currentWallpaperPath != todayWallpaperPath)
                {
                    SetWallpaper(todayWallpaperPath);
                    Console.WriteLine("壁纸设置成功！壁纸文件路径：" + todayWallpaperPath);
                }
                else
                {
                    Console.WriteLine("今天已经设置过壁纸啦！壁纸文件路径：" + todayWallpaperPath);
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
                    Console.WriteLine("\n今日壁纸获取失败，不更新壁纸。壁纸文件路径：" + currentWallpaperPath);
                    return;
                }
                Console.WriteLine();
            }   
        }
        
        Console.Write("按任意键结束。");
        Console.ReadKey();
    }

    // 获取当前壁纸的路径
    static string GetCurrentWallpaperPath()
    {
        // 打开注册表项 HKEY_CURRENT_USER\Control Panel\Desktop
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false);
        // 获取名为 "Wallpaper" 的键的值（即当前壁纸的路径）
        string wallpaperPath = key.GetValue("Wallpaper") as string;
        return wallpaperPath;
    }

    // 下载或检查是否已经下载了今天的壁纸
    static async Task<string> DownloadTodayWallpaper()
    {
        // 构造今天壁纸的文件名（格式为 yyyymmdd.jpg）
        string todayWallpaperFileName = $"{DateTime.Now:yyyyMMdd}.jpg";
        // 构造今天壁纸的保存路径（位于用户图片文件夹下的 BingWallpapers 文件夹中）
        string todayWallpaperPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SpotlightWallpapers", todayWallpaperFileName);

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
                // 发送 GET 请求以下载今天的壁纸
                HttpResponseMessage response = await httpClient.GetAsync("https://api.qzink.me/spotlight?orientation=landscape");

                // 如果下载成功，则将壁纸保存到本地并返回壁纸的路径
                if (response.IsSuccessStatusCode)
                {
                    // 读取响应内容的二进制数据
                    byte[] imageData = await response.Content.ReadAsByteArrayAsync();

                    // 构造壁纸保存的目录
                    string todayWallpaperDirectory = Path.GetDirectoryName(todayWallpaperPath);

                    // 如果目录不存在，则创建目录
                    if (!Directory.Exists(todayWallpaperDirectory))
                    {
                        Directory.CreateDirectory(todayWallpaperDirectory);
                    }

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

    // 调用 Windows API 的 SystemParametersInfo 函数
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}

