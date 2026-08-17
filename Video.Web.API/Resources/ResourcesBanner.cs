namespace Video.Web.API.Resources;

public static class ResourcesBanner
{
   public static void PrintStartupBanner()
    {
        try
        {
            var bannerPath = Path.Combine(AppContext.BaseDirectory, "Resources", "banner.txt");
            if (File.Exists(bannerPath))
            {
                Console.WriteLine(File.ReadAllText(bannerPath));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Identity] 启动 Banner 加载失败（不影响启动）: {ex.Message}");
        }
    }
}