using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppStoresScraper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var scraperFactory = new StoreScraperFactory();

            //Apple store
            StoreScrapeResult result = scraperFactory.ScrapeAsync("https://itunes.apple.com/us/app/logic-pro-x/id634148309?mt=12", true).Result;
            WriteJson(result);

            //Google Play store
            result = scraperFactory.ScrapeAsync("https://play.google.com/store/apps/details?id=com.google.android.talk", true).Result;
            WriteJson(result);

            //Windows store
            result = scraperFactory.ScrapeAsync("https://www.microsoft.com/en-us/store/apps/whos-next/9nblggh6d070", true).Result;
            WriteJson(result);

            //Steam store
            result = scraperFactory.ScrapeAsync("http://store.steampowered.com/app/364360/", true).Result;
            WriteJson(result);

            //Check if Steam store url
            if (scraperFactory.GetScraper("http://store.steampowered.com/app/364360") is SteamStoreScraper)
            {
                //<..>
            }

            //Get and call parser for specific store
            var scraper = scraperFactory.GetScraper<PlayStoreScraper>();
            var metadata = scraper.ScrapeAsync("com.android.chrome").Result;
            var icon = scraper.DownloadIconAsync(metadata).Result;
            ImageToAscii(icon.Content);

            var parsed = scraperFactory.ParseUrl("https://play.google.com/store/apps/details?id=com.android.chrome");
            WriteJson(parsed);

            //Add loggin
            scraperFactory = new StoreScraperFactory(logWritter: (level, s, ex) => { Console.WriteLine(level + ": " + s); }); 

            //invalid url
            result = scraperFactory.ScrapeAsync("https://itunes.apple.com/us/app/logic-pro-x/id123", true).Result;
            if (!result.IsSuccessful)
                Console.WriteLine("Failed (intended)");


            Console.ReadKey();
        }
 
        static void WriteJson(object obj)
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings {ContractResolver = new NoBloatResolver()});
            Console.WriteLine(json);
            var result = obj as StoreScrapeResult;
            if (result?.Icon?.Content != null)
                ImageToAscii(result.Icon.Content);
        }

        static void ImageToAscii(byte[] content)
        {
            var bmp = new Bitmap((int)(Console.WindowHeight/2m), (int)(Console.WindowHeight/2.5m));
            using (var graphics = Graphics.FromImage(bmp))
                graphics.DrawImage(Image.FromStream(new MemoryStream(content)), 0, 0, bmp.Width, bmp.Height);
            
            Console.WriteLine();
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color col = bmp.GetPixel(x, y);
                    col = Color.FromArgb((col.R + col.G + col.B)/3, (col.R + col.G + col.B)/3, (col.R + col.G + col.B)/3);
                    int rValue = int.Parse(col.R.ToString());
                    Console.Write(GetGrayShade(rValue));
                    if (x == bmp.Width - 1)
                        Console.WriteLine();
                }
            }
            Console.WriteLine();
        }

        static string GetGrayShade(int redValue)
        {
            var asciival = " ";
            if (redValue >= 230)
            {
                asciival = " ";
            }
            else if (redValue >= 200)
            {
                asciival = ".";
            }
            else if (redValue >= 180)
            {
                asciival = "`";
            }
            else if (redValue >= 160)
            {
                asciival = "|";
            }
            else if (redValue >= 130)
            {
                asciival = "*";
            }
            else if (redValue >= 100)
            {
                asciival = "^";
            }
            else if (redValue >= 80)
            {
                asciival = "?";
            }
            else if (redValue >= 60)
            {
                asciival = "8";
            }
            else if (redValue >= 50)
            {
                asciival = "#";
            }
            else
            {
                asciival = "@";
            }

            return asciival;
        }
    }
    class NoBloatResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
            return props.Where(p => p.PropertyType != typeof(byte[])).ToList();
        }
    }
}
