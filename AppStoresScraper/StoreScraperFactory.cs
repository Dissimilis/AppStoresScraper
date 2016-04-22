using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppStoresScraper
{
    public class StoreScraperFactory
    {
        private const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; AppStoresScraper/1.0; https://github.com/Dissimilis/AppStoresScraper)";

        /// <summary>
        /// HTTP client for making requests
        /// </summary>
        public HttpClient HttpClient { get; }

        public StoreScraperFactory(string userAgent = DefaultUserAgent)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            HttpClient = new HttpClient(handler);
            HttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            HttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.6");
            HttpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            HttpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        }

        /// <summary>
        /// Detects store type from URL and gets app metadata
        /// </summary>
        /// <param name="url">URL to app</param>
        /// <param name="downloadImages">If true, downloads app icon from store</param>
        /// <returns>Scraping result. Scraping exception are stored int result.Exception</returns>
        public async Task<StoreScrapeResult> ScrapeAsync(string url, bool downloadImages = false)
        {
            var scraper = GetScraper(url);
            var id = scraper?.GetIdFromUrl(url);
            return await ScrapeAsync(scraper?.Store ?? ScraperStoreType.Unknown, id, downloadImages);
        }

        /// <summary>
        /// Gets app metadata from provided store
        /// </summary>
        /// <param name="appId">App id in store</param>
        /// <param name="downloadImages">If true, downloads app icon from store</param>
        /// <param name="type">App store type</param>
        /// <returns>Scraping result. Scraping exception are stored int result.Exception</returns>
        public async Task<StoreScrapeResult> ScrapeAsync(ScraperStoreType type, string appId, bool downloadImages = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new StoreScrapeResult();
            var scraper = GetScraper(type);
            if (scraper == null)
                return result;
            result.Store = scraper.Store;
            result.AppId = appId;
            if (string.IsNullOrEmpty(appId))
                return result;
            try
            {
                result.Metadata = await scraper.ScrapeAsync(appId);
                result.Metadata.StoreType = result.Store;
                if (result.Metadata?.IconUrl != null && result.Metadata.IconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    result.Icon = await scraper.DownloadIconAsync(result.Metadata);
                }
                if (string.IsNullOrWhiteSpace(result.Metadata.Name) || string.IsNullOrWhiteSpace(result.Metadata.IconUrl))
                    result.Exception = new ScraperException(scraper.Store + " scraper failed to parse result", scraper.Store, appId, result.Metadata.AppUrl);
            }
            catch (Exception ex) //should probably only catch WebException
            {
                var wex = ex as WebException;
                var httpResponse = wex?.Response as HttpWebResponse;
                string url = null;
                int? statusCode = null;
                if (httpResponse != null)
                {
                    statusCode = (int)httpResponse.StatusCode;
                    url = httpResponse.ResponseUri.AbsoluteUri;
                }
                result.Exception = new ScraperException(scraper.Store + " scraper failed", scraper.Store, appId, url, statusCode);
            }
            result.ParseTime = sw.Elapsed;
            return result;
        }

        /// <summary>
        /// Analyzes URL and returns specific store scraper
        /// </summary>
        /// <param name="url">URL of app</param>
        /// <returns>null when URL is invalid</returns>
        public IStoreScraper GetScraper(string url)
        {
            var type = GetStoreType(url);
            if (type == ScraperStoreType.Unknown)
                return null;
            return GetScraper(type);
        }

        /// <summary>
        /// Gets scraper instance from store type enum
        /// </summary>
        /// <returns>Specific scraper instance</returns>
        public IStoreScraper GetScraper(ScraperStoreType store)
        {
            switch (store)
            {
                case ScraperStoreType.PlayStore: return new PlayStoreScraper(HttpClient);
                case ScraperStoreType.AppleStore: return new AppleStoreScraper(HttpClient);
                case ScraperStoreType.WindowsStore: return new WindowsStoreScraper(HttpClient);
                default: return null;
            }
        }

        /// <summary>
        /// Checks if URL is valid for any of supported scrapers (whithout making request to app store)
        /// </summary>
        /// <param name="url">URL to parse</param>
        /// <returns>Normalized URL and AppId; null if URL is invalid</returns>
        public AppIdentification ParseUrl(string url)
        {
            var store = GetStoreType(url);
            var scraper = GetScraper(store);
            var id = scraper?.GetIdFromUrl(url);
            if (string.IsNullOrEmpty(id))
                return null;
            return new AppIdentification() { Id = id, AppUrl = scraper.GetUrlFromId(id), StoreType = store };
        }

        /// <summary>
        /// Constructs standard deterministic URL to app store from store type and app id in that store
        /// </summary>
        /// <param name="store">Store type</param>
        /// <param name="appId">App id in store</param>
        /// <returns>Full URL to app (ignores locality)</returns>
        public string GetNormalizedUrl(ScraperStoreType store, string appId)
        {
            var scraper = GetScraper(store);
            return scraper?.GetUrlFromId(appId);
        }

        /// <summary>
        /// Analyzes app URL and returns store type enum
        /// </summary>
        public ScraperStoreType GetStoreType(string url)
        {
            if (string.IsNullOrEmpty(url))
                return ScraperStoreType.Unknown;
            if (url.StartsWith("https://play.google.com/store/", StringComparison.OrdinalIgnoreCase) || url.StartsWith("market://details?id=", StringComparison.OrdinalIgnoreCase))
                return ScraperStoreType.PlayStore;
            if (url.StartsWith("https://itunes.apple.com/", StringComparison.OrdinalIgnoreCase))
                return ScraperStoreType.AppleStore;
            if (Regex.IsMatch(url, @"http.*?://w*?\.*?microsoft.com/.*?/?store/.+", RegexOptions.IgnoreCase))
                return ScraperStoreType.WindowsStore;
            return ScraperStoreType.Unknown;
        }
    }
}