using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Flight_tracker
{
    class MakeTweet
    {
        static Browser browser;
        static Page page;
        static async Task launchBrowser()
        {
            LaunchOptions ops = new LaunchOptions();
            ops.UserDataDir = Directory.GetCurrentDirectory() + "\\cache";
            ops.Headless = true;
            new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            browser = await Puppeteer.LaunchAsync(ops);

            page = (await browser.PagesAsync())[0];

            page.DefaultTimeout = 0;
            var navigation = new NavigationOptions
            {
                Timeout = 0,
                WaitUntil = new[] {
                        WaitUntilNavigation.Networkidle2 }
            };
            ViewPortOptions v = new ViewPortOptions();
            v.Width = 1920;
            v.Height = 1000;
            await page.SetViewportAsync(v);
            await page.GoToAsync("https://twitter.com/", navigation);
        }
        static async Task Main(string[] args)
        {
            await launchBrowser();
            await makeTweetAsync("Hello, I am currently still working on the automation of this account updates on Putin's jet will start soon!");
        }
        public static async Task makeTweetAsync(String str)
        {
            await page.SetCacheEnabledAsync(true);
            await page.SetJavaScriptEnabledAsync(true);
            await page.ClickAsync("a[data-testid='SideNav_NewTweet_Button']");
            await page.ClickAsync("div[data-testid='tweetTextarea_0']");
            TypeOptions op = new TypeOptions();
            op.Delay = 10;
            await page.TypeAsync("div[data-testid='tweetTextarea_0']", str,op);
            await page.ClickAsync("div[data-testid='tweetButton']");
            await page.WaitForNavigationAsync();
            //await browser.CloseAsync();
        }
    }
}
