using HtmlAgilityPack;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Flight_tracker
{
    class FlightInfo
    {
        public String landLocation;
        public String flightTime;
        public String takeOffLocation;
        public String status;
        public String date;
        public String arrivalTime;
        public String statusTime;
        public override string ToString()
        {
            String str = "";

            str += $"Date: {date}\n";
            str += $"Flight time: {flightTime}\n";
            str += $"Take off location: {takeOffLocation}\n";
            str += $"Land location: {landLocation}\n";
            str += $"Status: {status}\n";
            str += $"Arrival time: {arrivalTime}\n";
            return str;
        }
        public override bool Equals(object obj)
        {
            FlightInfo comp = (FlightInfo)obj;
            bool isEqual = true;
            isEqual &= landLocation.Equals(comp.landLocation);
            isEqual &= flightTime.Equals(comp.flightTime);
            isEqual &= takeOffLocation.Equals(comp.takeOffLocation);
            isEqual &= status.Equals(comp.status);
            isEqual &= arrivalTime.Equals(comp.arrivalTime);
            isEqual &= statusTime.Equals(comp.statusTime);
            return isEqual;
        }
    }
    class FlightTracker
    {
        static async Task<List<FlightInfo>> fetchAllFlights(String url)
        {
            String page = (await FetchPageAsync(url));
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);
            List<FlightInfo> flightList = new List<FlightInfo>();
            var tables = doc.DocumentNode.SelectNodes("/html/body/div[7]/div[2]/section/section[2]/div[4]/table/tbody//tr[@class=' data-row']");
            foreach(var n in tables)
            {
                FlightInfo info = new FlightInfo();
                info.takeOffLocation = n.SelectSingleNode("td[1]/div/div[3]/p[1]/span").InnerText;
                info.flightTime = n.SelectSingleNode("td/div/div[1]/div[3]").InnerText;
                info.arrivalTime = n.SelectSingleNode("td/div/div[3]/div/div[3]/p/span").InnerText;
                info.landLocation = n.SelectSingleNode("td/div/div[3]/p[2]/span").InnerText;
                info.status = n.SelectSingleNode("td[1]/div/div[1]/div[4]").InnerText;
                info.date = n.SelectSingleNode("td[1]/div/div[1]/div[2]").InnerText;
                cleanUpFlightInfo(info);
                flightList.Add(info);
            }
            return flightList;
        }
        static FlightInfo cleanUpFlightInfo(FlightInfo info)
        {
            if (info.flightTime != null)
                info.flightTime = info.flightTime.Substring(1);
            else
                info.flightTime = "-";

            if (info.date != null)
                info.date = info.date.Substring(1);
            else
                info.date = "-";

            if (info.takeOffLocation != null)
                info.takeOffLocation = info.takeOffLocation.Substring(2);
            else
                info.takeOffLocation = "-";

            if (info.landLocation != null)
                info.landLocation = info.landLocation.Substring(2);
            else
                info.landLocation = "-";

            if (info.status != null)
                info.status = info.status.Substring(1);
            else
                info.status = "-";
            return info;
        }
        static async Task<FlightInfo> getInfo(String url)
        {
            String page = await FetchPageAsync(url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);
            FlightInfo info = new FlightInfo();
            info.date = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[4]/table/tbody/tr/td[1]/div/div[1]/div[2]").InnerText;
            info.takeOffLocation = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[4]/table/tbody/tr/td[1]/div/div[3]/p[1]/span").InnerText;
            info.flightTime = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[4]/table/tbody/tr/td/div/div[1]/div[3]").InnerText;
            info.arrivalTime = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[4]/table/tbody/tr/td/div/div[3]/div/div[3]/p/span").InnerText;
            info.landLocation = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[4]/table/tbody/tr/td/div/div[3]/p[2]/span").InnerText;
            String fullStatusString = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[4]/table/tbody/tr/td[1]/div/div[1]/div[4]").InnerText;
            info.status = fullStatusString.Substring(0, fullStatusString.LastIndexOf(' '));
            info.statusTime = fullStatusString.Substring(fullStatusString.IndexOf(" ") + 1);
            return info;
        }
        static void Main(string[] args)
        {
            String reg = "n11192";
            var info = fetchAllFlights("https://"+$"www.flightradar24.com/data/aircraft/{reg}").GetAwaiter().GetResult();
            
            foreach (FlightInfo f in info)
            {
                Console.WriteLine(f.ToString());
            }
        }
        public static async Task<String> FetchPageAsync(String url)
        {
            BrowserFetcher f = new BrowserFetcher();
            await f.DownloadAsync();
            Browser browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });

            var page = await browser.NewPageAsync();

            page.DefaultTimeout = 0;
            var navigation = new NavigationOptions
            {
                Timeout = 0,
                WaitUntil = new[] {
                        WaitUntilNavigation.Networkidle0 }
            };
            await page.SetJavaScriptEnabledAsync(true);
            await page.GoToAsync(url, navigation);
            return await page.GetContentAsync();
        }
    }
    
}
