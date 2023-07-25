using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using Serilog;

namespace MockWebClient.Models
{
    /// <summary>
    /// 瀏覽事件
    /// </summary>
    public class BrowseEvent
    {
        /// <summary>
        /// 等待時間
        /// </summary>
        private const int IMPLICITWAITSECONDS = 30;

        /// <summary>
        /// 預設等待瀏覽時間
        /// </summary>
        private const int DEFAULTWAITSECONDS = 10;

        /// <summary>
        /// 預設domain
        /// </summary>
        public Uri? BaseUri { get; set; }

        /// <summary>
        /// Selenium - 瀏覽器核心 
        /// </summary>
        public IWebDriver WebDriver { get; private set; }

        /// <summary>
        /// Logger
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Selenium - 操作瀏覽器動作
        /// </summary>
        public INavigation Navigate => this.WebDriver.Navigate();

        /// <summary>
        /// Selenium - 操作使用者動作
        /// </summary>
        public IOptions Manager => this.WebDriver.Manage();

        /// <summary>
        /// 忽略的網址
        /// </summary>
        public HashSet<Uri> Skips { get; set; } = new();

        /// <summary>
        /// 瀏覽記錄 Pool
        /// </summary>
        public HashSet<Uri> Records { get; set; } = new();

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="driver">Driver</param>
        /// <param name="logger">Logger</param>
        public BrowseEvent(IWebDriver driver, ILogger logger)
        {
            this.WebDriver = driver;
            this.Logger = logger;
        }

        /// <summary>
        /// Driect Hit
        /// </summary>
        /// <param name="url">網址</param>
        public void DirectOpenUrl(Uri url) => DirectOpenUrl(url.ToString());

        /// <summary>
        /// Driect Hit
        /// </summary>
        /// <param name="url">網址</param>
        public void DirectOpenUrl(string url)
        {
            url = url.TrimEnd('/');
            if (Uri.TryCreate(url, UriKind.Absolute, out var temp))
            {
                this.Records.Add(temp);
                var waitSecs = RandomNumberGenerator.GetInt32(DEFAULTWAITSECONDS);
                this.Logger.Information($"瀏覽 {temp} - 等待 {waitSecs} 秒");
                this.Navigate.GoToUrl(temp);
                this.Manager.Timeouts().ImplicitWait = TimeSpan.FromSeconds(IMPLICITWAITSECONDS);

                Thread.Sleep(TimeSpan.FromSeconds(waitSecs));
            }
        }

        /// <summary>
        /// 加入忽略的網址
        /// </summary>
        /// <param name="uri">網址</param>
        public void AddIgnoreUrl(string uri) => AddIgnoreUrl(new Uri(uri));

        /// <summary>
        /// 加入忽略的網址
        /// </summary>
        /// <param name="uri">網址</param>
        public void AddIgnoreUrl(Uri uri)
        {
            this.Skips.Add(uri);
        }
    }
}