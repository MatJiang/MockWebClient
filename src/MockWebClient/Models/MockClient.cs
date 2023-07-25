using MockWebClient.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;

namespace MockWebClient;

/// <summary>
/// Client
/// </summary>
public class MockClient : IDisposable
{
    /// <summary>
    /// 使用的瀏覽器 Driver
    /// </summary>
    public IWebDriver WebDriver { get; }

    /// <summary>
    /// Logger
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// 瀏覽事件
    /// </summary>
    public event EventHandler<BrowseEvent> BrowseEvents;

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="webDriver">瀏覽器核心</param>
    public MockClient(IWebDriver webDriver, ILogger logger)
    {
        WebDriver = webDriver ?? throw new ArgumentNullException(nameof(webDriver));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        BrowseEvents += (sender, e) => { };
    }


    /// <summary>
    /// 關閉瀏覽器
    /// </summary>
    public void CloseWebDriver() => this.WebDriver?.Quit();

    /// <summary>
    /// 清除Cookies
    /// </summary>
    public void ClearCookies() => this.WebDriver?.Manage().Cookies.DeleteAllCookies();

    /// <summary>
    /// 清除Cookie
    /// </summary>
    /// <param name="name">Cookie Name</param>
    public void ClearCookieByName(string name) => this.WebDriver?.Manage().Cookies.DeleteCookieNamed(name);

    /// <summary>
    /// 印出SC_ANALYTICS_GLOBAL_COOKIE
    /// </summary>
    public void PrintSitecoreAnalyticCookie() => PrintCookieByName("SC_ANALYTICS_GLOBAL_COOKIE");

    private void PrintCookieByName(string name) => Logger.Information($"{name} : " + this.WebDriver?.Manage().Cookies.GetCookieNamed(name)?.Value ?? $"查無 {name} Cookie");

    /// <summary>
    /// 瀏覽
    /// </summary>
    public void OnBrowse()
    {
        var eventArg = new BrowseEvent(WebDriver, Logger);
        try
        {
            BrowseEvents?.Invoke(this, eventArg);
        }
        catch
        {
            Dispose();
        }
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        this.CloseWebDriver();
        GC.SuppressFinalize(this);
    }
}
