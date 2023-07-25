using System.Security.Cryptography;
using MockWebClient.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using Serilog;

namespace MockWebClient;

/// <summary>
/// 瀏覽程式庫
/// </summary>
public class BrowseRepository
{
    /// <summary>
    /// Logger
    /// </summary>
    public ILogger Logger { get; set; }

    private static readonly string Scheme = "https";
    private static readonly string BaseDomain = "{Base Domain}";
    private static readonly IList<string> EntryUrls = new List<string>() { "{入口頁面1}", "{入口頁面2}" };
    private static readonly IList<string> IgnoreUrls = new List<string>() { "{忽略Path1}", "{忽略Path2}" };
    private const int TOTALMAXPAGESCOUNT = 10;
    private const int REVIEWSCOUNT = 5;
    public static BrowseRepository Default => new(
        new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger()
    );

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="logger">Logger</param>
    public BrowseRepository(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 程式進入點 開始瀏覽
    /// </summary>
    /// <param name="count">總模擬人數</param>
    public void DoBrowse(int count)
    {
        Logger.Information($"使用者 {count} 人");
        var chromeOption = new ChromeOptions();
        chromeOption.AddArgument("ignore-certificate-errors");
        for (int i = 0; i < count; i++)
        {
            using (var client = new MockClient(new ChromeDriver(chromeOption), Logger))
            {
                client.BrowseEvents += SetIgnoreUrls;
                var viewTimes = RandomNumberGenerator.GetInt32(1, REVIEWSCOUNT);
                Logger.Information($"第 {i + 1} 位使用者 - 瀏覽 {viewTimes} 次");
                for (int j = 0; j < viewTimes; j++)
                {
                    client.BrowseEvents += OpenEntryUrl;
                    client.BrowseEvents += BrowsePages;
                    if (viewTimes > 1)
                    {
                        client.BrowseEvents += ResetClient;
                    }
                }

                client.OnBrowse();
                client.PrintSitecoreAnalyticCookie();
            }
        }
    }

    /// <summary>
    /// 設定忽略的網址
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="e">event</param>
    public static void SetIgnoreUrls(object? sender, BrowseEvent e)
    {
        foreach (var path in IgnoreUrls)
        {
            UriBuilder uriBuilder = new(Scheme, BaseDomain, -1, path.TrimEnd('/'));
            e.AddIgnoreUrl(uriBuilder.Uri);
        }
    }

    /// <summary>
    /// 重置Session，模擬使用者進入下一次瀏覽動作
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="e">event</param>
    public void ResetClient(object? sender, BrowseEvent e)
    {
        e.Records = new HashSet<Uri>();
        e.Manager.Cookies.DeleteCookieNamed("ASP.NET_SessionId");
        Logger.Information($"清除 Session，重置瀏覽");
    }

    /// <summary>
    /// 開啟入口網頁，爬取連結起始位置
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="e">event</param>
    public static void OpenEntryUrl(object? sender, BrowseEvent e)
    {
        var driver = e.WebDriver;
        var idx = RandomNumberGenerator.GetInt32(0, EntryUrls.Count);
        UriBuilder uriBuilder = new(Scheme, BaseDomain, -1, EntryUrls[idx]);
        e.BaseUri = uriBuilder.Uri;
        e.DirectOpenUrl(uriBuilder.ToString());
        ActionBuilder actionBuilder = new ActionBuilder();
        PointerInputDevice mouse = new PointerInputDevice(PointerKind.Mouse, "default mouse");
        actionBuilder.AddAction(mouse.CreatePointerMove(CoordinateOrigin.Viewport,
            8, 0, TimeSpan.Zero));
        ((IActionExecutor)driver).PerformActions(actionBuilder.ToActionSequenceList());
    }

    /// <summary>
    /// 瀏覽數頁
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="e">event</param>
    public void BrowsePages(object? sender, BrowseEvent e)
    {
        // 總瀏覽頁數
        var totalPages = RandomNumberGenerator.GetInt32(2, TOTALMAXPAGESCOUNT + 1);
        Logger.Information($"瀏覽 {totalPages} 頁");
        var records = e.Records;
        var driver = e.WebDriver;
        // 瀏覽所有頁面
        while (e.Records.Count <= totalPages)
        {
            try
            {
                IWebElement element = driver.FindElement(By.TagName("body"));
                IList<IWebElement> elements = element.FindElements(By.TagName("a"));

                if (element == null || elements == null || elements.Count == 0)
                {
                    e.DirectOpenUrl(e.BaseUri?.ToString() ?? "/");
                    continue;
                }

                var idx = RandomNumberGenerator.GetInt32(0, elements.Count);

                var item = elements[idx];
                var href = item.GetAttribute("href");
                var target = item.GetAttribute("target");

                // 無連結屬性
                if (href == null
                || href.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
                || href.Contains("mailto:", StringComparison.OrdinalIgnoreCase)
                || href.Contains("tel:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                //當前頁面錨點 or 當前頁面
                if (href.StartsWith(driver.Url + "#", StringComparison.OrdinalIgnoreCase) || href.Equals(driver.Url, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 不正常的連結
                if (!Uri.TryCreate(href, UriKind.Absolute, out var url))
                {
                    continue;
                }

                // 外部Domain
                if (url.Host != e.BaseUri?.Host)
                {
                    continue;
                }

                // 排除 Sitecore media
                if (href.Contains("/-/media/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 跳過 or 已經執行過
                if (e.Skips.Any(u => u.Equals(url)) || e.Records.Any(u => u.Equals(url)))
                {
                    continue;
                }

                new Actions(driver).MoveToElement(item).Perform();
                e.DirectOpenUrl(url);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, ex.Message);
            }
        }
    }
}
