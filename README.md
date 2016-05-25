# AppStoresScraper
Steam, Apple, Google and Windows App stores scraper/parser

Downloads most essential info about app (including icon) from ITunes, Play store, Windows store and Steam store

Uses .NET 4.5 and C# 6. Methods are async

**Usage examples:**
```csharp
StoreScraperFactory scraperFactory = new StoreScraperFactory();

//Steam store
StoreScrapeResult result = await scraperFactory.ScrapeAsync("http://store.steampowered.com/app/231430/", true);

//Apple store
StoreScrapeResult result = await scraperFactory.ScrapeAsync("https://itunes.apple.com/us/app/logic-pro-x/id634148309?mt=12", true);

//Google Play store
StoreScrapeResult result = await scraperFactory.ScrapeAsync("https://play.google.com/store/apps/details?id=com.google.android.talk", true);

//Windows store
StoreScrapeResult result = await scraperFactory.ScrapeAsync("https://www.microsoft.com/en-us/store/apps/circle-rush/9nblggh0cdmf", true);

//Check URL type
if (scraperFactory.GetScraper("http://store.steampowered.com/app/364360") is SteamStoreScraper) { }

//Get and call scraper for specific store
IStoreScraper scraper = scraperFactory.GetScraper<PlayStoreScraper>();
AppMetadata metadata = await scraper.ScrapeAsync("com.android.chrome");
var icon = await scraper.DownloadIconAsync(metadata);

//Parse URL
var parsed = scraperFactory.ParseUrl("https://play.google.com/store/apps/details?id=com.android.chrome");
//returns store type and app id