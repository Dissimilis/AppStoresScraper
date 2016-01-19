# AppStoresScraper
Apple, Google and Windows App stores scraper/parser

Downloads most essential info about app (including icon) from ITunes, Play store and Windows store

Uses .NET 4.5 and C# 6. Methods are async

**Usage examples:**
```csharp
var scraperFactory = new StoreScraperFactory();

//Apple store
var result = scraperFactory.Scrape("https://itunes.apple.com/us/app/logic-pro-x/id634148309?mt=12", true).Result;

//Google Play store
result = scraperFactory.Scrape("https://play.google.com/store/apps/details?id=com.google.android.talk", true).Result;

//Windows store
result = scraperFactory.Scrape("https://www.microsoft.com/en-us/store/apps/circle-rush/9nblggh0cdmf", true).Result;

//Get store type from URL
var storeType = scraperFactory.GetScraper("https://play.google.com/store/apps/details?id=com.android.chrome").Store;

//Get and call scraper for specific store
var scraper = scraperFactory.GetScraper(StoreType.PlayStore);
var metadata = scraper.Scrape("com.android.chrome").Result;
var icon = scraper.DownloadIcon(metadata).Result;
