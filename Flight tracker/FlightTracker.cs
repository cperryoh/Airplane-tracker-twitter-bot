using HtmlAgilityPack;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Flight_tracker
{
    enum Status
    {
        Laneded,
        Airborne,
        Scheduled,
        Unknown
    };
    class JetInfo
    {
        public string aircraft { get; set; }
        public string op { get; set; }
        public string airline { get; set; }
        public override string ToString()
        {
            return $"Aircraft info\n---------\nAircraft: {aircraft}\nOperator: {op}\nAirline: {airline}\n";
        }
        static String clean(String str)
        {
            return str.Substring(2, str.Length - 4);
        }
        public static JetInfo getJetInfo(String page)
        {
            JetInfo info = new JetInfo();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);
            info.aircraft = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[1]/div[1]/div[2]/div[1]/div[1]/span").InnerText;
            info.op = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[1]/div[1]/div[2]/div[1]/div[3]/span").InnerText;
            info.airline = doc.DocumentNode.SelectSingleNode("/html/body/div[7]/div[2]/section/section[2]/div[1]/div[1]/div[2]/div[1]/div[2]/span/a").InnerText;

            info.aircraft = clean(info.aircraft);
            info.airline = clean(info.airline);
            info.op = clean(info.op);
            return info;
        }
    }
    class FlightInfo
    {
        public String landLocation;
        public String flightTime;
        public String takeOffLocation;
        public DateTime scheduledArrival;
        public DateTime actualArrival;
        public DateTime actualDeparture;
        public Status status;
        void printInColor(String str,ConsoleColor color)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(str);
            Console.ForegroundColor = temp;
        }
        public void print()
        {
            printInColor("\nFlight info\n-------------\n",ConsoleColor.Cyan);
            if (!flightTime.Equals(null))
                Console.Write($"Flight time: {flightTime}\n");

            Console.Write($"Take off location: {takeOffLocation}\n");
            Console.Write($"Land location: {landLocation}\n");

            switch (status) {
                case Status.Airborne:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case Status.Laneded:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case Status.Scheduled:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case Status.Unknown:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }

            Console.Write($"Status: {status}\n");
            Console.ForegroundColor = ConsoleColor.White;

            if (!actualArrival.Equals(DateTime.MinValue))
                Console.Write($"Actual Arrival time: {actualArrival.ToString("[MM/dd/yy] hh:mm tt")}\n");

            if (!scheduledArrival.Equals(DateTime.MinValue))
                Console.Write($"Scheduled Arrival time: {scheduledArrival.ToString("[MM/dd/yy] hh:mm tt")}\n");

            if (!actualDeparture.Equals(DateTime.MinValue))
                Console.Write($"Departure time: {actualDeparture.ToString("[MM/dd/yy] hh:mm tt")}\n");

            if (status == Status.Airborne)
            {
                DateTime now = getUTC(DateTime.Now);
                TimeSpan dif = scheduledArrival.Subtract(now);
                Console.Write($"Time till land(hrs:min): {dif.Hours}:{dif.Minutes}\n");
            }
        }
        public DateTime getUTC(DateTime time)
        {
            DateTime now = DateTime.Now;
            return now.ToUniversalTime();
        }
        public override bool Equals(object obj)
        {
            FlightInfo comp = (FlightInfo)obj;
            bool isEqual = true;
            return isEqual;
        }
    }
    class FlightTracker
    {
        static DateTime fixDate(DateTime date, DateTime time,DateTime refernce)
        {
            DateTime final;
            if (!time.Equals(DateTime.MinValue))
            {
                final = date.Date.Add(time.TimeOfDay);
                if (refernce.ToString("tt").Equals("PM") && time.ToString("tt").Equals("AM"))
                    final = final.AddDays(1.0);
            }
            else
                final = DateTime.MinValue;
            return final;
        }
        static DateTime fixDate(DateTime date, DateTime time)
        {
            DateTime final;
            if (!time.Equals(DateTime.MinValue))
            {
                final = date.Date.Add(time.TimeOfDay);
            }
            else
                final = DateTime.MinValue;
            return final;
        }
        static Status convertToEnum(String str)
        {
            if (str.Equals("Landed"))
                return Status.Laneded;
            if (str.Equals("Estimated departure ") || str.Equals("Scheduled"))
                return Status.Scheduled;
            if (str.Equals("Estimated"))
                return Status.Airborne;
            return Status.Unknown;
        }
        static FlightInfo getLiveData(HtmlNode n)
        {
            FlightInfo info = new FlightInfo();
            //pull strings
            info.takeOffLocation = n.SelectSingleNode("td[4]").InnerText;
            info.flightTime = "  —  ";
            String status = n.SelectSingleNode("td[1]/div/div[1]/div[4]").InnerText.Substring(1);
            info.landLocation = n.SelectSingleNode("td[5]").InnerText;
            info.status =Status.Airborne;

            //parse actual dep time
            String depTimeStr = n.SelectSingleNode("td[9]").InnerText;
            DateTime actualDepartureTime;
            if (!depTimeStr.Equals("  —  "))
                actualDepartureTime = DateTime.Parse(depTimeStr);
            else
                actualDepartureTime = DateTime.MinValue;

            //scheduled dep time
            String scheduledTime = n.SelectSingleNode("td[1]/div/div[3]/div/div[1]/p/span").InnerText;
            DateTime scheduledDepTime;
            if (!scheduledTime.Equals("  —  "))
                scheduledDepTime = DateTime.Parse(scheduledTime);
            else
                scheduledDepTime = DateTime.MinValue;


            //parse arrival time
            DateTime scheduledArrivalTime;
            String scheduledArrivalTimeStr = n.SelectSingleNode("td[10]").InnerText;
            if (!scheduledArrivalTimeStr.Equals("  —  "))
                scheduledArrivalTime = DateTime.Parse(scheduledArrivalTimeStr);
            else
                scheduledArrivalTime = DateTime.MinValue;

            //parse date
            DateTime date = DateTime.Parse(n.SelectSingleNode("td[3]").InnerText);

            //combine date and time 
            DateTime scheduledDepTimeFull = fixDate(date,scheduledDepTime);
            DateTime scheduledFullArrival=fixDate(date,scheduledArrivalTime, scheduledDepTimeFull);
            DateTime actualDepartureTimeFull= fixDate(date, actualDepartureTime, scheduledDepTimeFull);


            info.actualDeparture = actualDepartureTimeFull;
            info.scheduledArrival = scheduledFullArrival;
            info.actualArrival = DateTime.MinValue;
            cleanUpFlightInfo(info);
            return info;
        }
        static async Task<List<FlightInfo>> fetchAllFlights(String url)
        {
            String page = (await FetchPageAsync(url));
            HtmlDocument doc = new HtmlDocument();
            JetInfo jet = JetInfo.getJetInfo(page);
            Console.WriteLine(jet);
            doc.LoadHtml(page);
            List<FlightInfo> flightList = new List<FlightInfo>();
            var tables = doc.DocumentNode.SelectNodes("//tr[contains(@class,'data-row')]");
            HtmlNode liveNode=null;
            foreach (var n in tables)
            {
                FlightInfo info = new FlightInfo();

                //pull strings
                info.takeOffLocation = n.SelectSingleNode("td[1]/div/div[3]/p[1]/span").InnerText;
                info.flightTime = n.SelectSingleNode("td[1]/div/div[1]/div[3]").InnerText;
                info.landLocation = n.SelectSingleNode("td/div/div[3]/p[2]/span").InnerText;
                String status = n.SelectSingleNode("td[1]/div/div[1]/div[4]").InnerText.Substring(1);

                String parsedStatus = status.Substring(0, status.Length-1);
                info.status = convertToEnum(parsedStatus.Substring(0, parsedStatus.LastIndexOf(" ")));

                if (info.status != Status.Airborne)
                {
                    String depTimeStr = n.SelectSingleNode("td[1]/div/div[3]/div/div[2]/p/span").InnerText;
                    DateTime actualDepartureTime;

                    //parse actual dep
                    if (!depTimeStr.Equals("  —  "))
                        actualDepartureTime = DateTime.Parse(depTimeStr);
                    else
                        actualDepartureTime = DateTime.MinValue;


                    //parse actual arrival
                    DateTime actualArrivalTime;
                    if (info.status != Status.Scheduled && info.status != Status.Unknown)
                        actualArrivalTime = DateTime.Parse(parsedStatus.Substring(parsedStatus.LastIndexOf(" ")));
                    else
                        actualArrivalTime = DateTime.MinValue;


                    //parse schedualed arrival
                    DateTime scheduledArrivalTime;
                    String scheduledArrivalTimeStr = n.SelectSingleNode("td[1]/div/div[3]/div/div[3]/p/span").InnerText;
                    if (!scheduledArrivalTimeStr.Equals("  —  "))
                        scheduledArrivalTime = DateTime.Parse(scheduledArrivalTimeStr);
                    else
                        scheduledArrivalTime = DateTime.MinValue;


                    //parse date
                    DateTime date = DateTime.Parse(n.SelectSingleNode("td[3]").InnerText);

                    //combine date and time 
                    DateTime scheduledFullArrival = fixDate(date, scheduledArrivalTime, actualDepartureTime);
                    DateTime actualDepartureTimeFull = fixDate(date, actualDepartureTime);
                    DateTime actualFullArrival = fixDate(date, actualArrivalTime, actualDepartureTime);


                    info.actualDeparture = actualDepartureTimeFull;
                    info.scheduledArrival = scheduledFullArrival;

                    info.actualArrival = (info.status != Status.Airborne) ? actualFullArrival : DateTime.MinValue;
                    cleanUpFlightInfo(info);
                    flightList.Add(info);
                }
                else
                {
                    liveNode = n;
                }
            }
            if (liveNode != null)
                flightList.Add(getLiveData(liveNode));
            Comparison<FlightInfo> comparison = (x, y) => DateTime.Compare(x.scheduledArrival, y.scheduledArrival);
            flightList.Sort(comparison);
            return flightList;
        }
        static FlightInfo cleanUpFlightInfo(FlightInfo info)
        {
            if (info.flightTime != null)
                info.flightTime = info.flightTime.Substring(1);
            else
                info.flightTime = "-";

            if (info.takeOffLocation != null)
                info.takeOffLocation = info.takeOffLocation.Substring(2);
            else
                info.takeOffLocation = "-";

            if (info.landLocation != null)
                info.landLocation = info.landLocation.Substring(2);
            else
                info.landLocation = "-";
            return info;
        }
        static void writeLines(String[] lines)
        {
            foreach (String str in lines)
                Console.WriteLine(str);
        }
        static void Main(string[] args)
        {
            String reg = "RA-96022";
            String curDir = Directory.GetCurrentDirectory();

            String[] open = File.ReadAllLines(curDir + "\\bigfunny.txt");
            writeLines(open);
            var info = fetchAllFlights("https://" + $"www.flightradar24.com/data/aircraft/{reg}").GetAwaiter().GetResult();
            foreach (FlightInfo f in info)
            {
                f.print();
            }
        }
        public static async Task<String> FetchPageAsync(String url)
        {
            LaunchOptions ops = new LaunchOptions();
            ops.UserDataDir = Directory.GetCurrentDirectory() + "\\cache";
            ops.Headless = false;
            Browser browser = await Puppeteer.LaunchAsync(ops);

            var page = await browser.NewPageAsync();

            page.DefaultTimeout = 0;
            var navigation = new NavigationOptions
            {
                Timeout = 0,
                WaitUntil = new[] {
                        WaitUntilNavigation.Networkidle0 }
            };
            ViewPortOptions v = new ViewPortOptions();
            v.Width = 1920;
            v.Height = 1000;
            await page.SetViewportAsync(v);
            await page.GoToAsync(url, navigation);
            await page.SetCacheEnabledAsync(true);

            await page.SetJavaScriptEnabledAsync(true);
            await page.ClickAsync("button[data-testid='cookie-consent-bar-close']");
            await page.ClickAsync("li[id='fr24_SettingsMenu']");
            await page.ClickAsync("a[data-target='#timesettings']");
            await page.ClickAsync("div[id='fr24_showLocalTime']");
            await page.ClickAsync("li[id='fr24_SettingsMenu']");
            //await page.ReloadAsync();
            String value = await page.GetContentAsync();
            // browser.CloseAsync();
            return value;
        }
    }

}
