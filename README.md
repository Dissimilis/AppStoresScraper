# AppStoresScraper
Steam, Apple, Google and Windows App stores scraper/parser

Downloads most essential info about app (including icon) from ITunes, Play store, Windows store and Steam store

Uses .NET 4.5 and C# 6. Methods are async

**Usage examples:**
```csharp
var scraperFactory = new StoreScraperFactory();

//Steam store
var result = scraperFactory.ScrapeAsync("http://store.steampowered.com/app/231430/", true).Result;

//Apple store
var result = scraperFactory.ScrapeAsync("https://itunes.apple.com/us/app/logic-pro-x/id634148309?mt=12", true).Result;

//Google Play store
result = scraperFactory.ScrapeAsync("https://play.google.com/store/apps/details?id=com.google.android.talk", true).Result;

//Windows store
result = scraperFactory.ScrapeAsync("https://www.microsoft.com/en-us/store/apps/circle-rush/9nblggh0cdmf", true).Result;

//Check URL type
if (scraperFactory.GetScraper("http://store.steampowered.com/app/364360") is SteamStoreScraper) { }

//Get and call scraper for specific store
var scraper = scraperFactory.GetScraper<PlayStoreScraper>();
var metadata = scraper.ScrapeAsync("com.android.chrome").Result;
var icon = scraper.DownloadIcon(metadata).Result;

//Parse URL
var parsed = scraperFactory.ParseUrl("https://play.google.com/store/apps/details?id=com.android.chrome");
//returns store type and app id