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
        public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; AppStoresScraper/1.0; https://github.com/Dissimilis/AppStoresScraper)";

        private HttpClient _client;
        public StoreScraperFactory(string userAgent = null)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Add("User-Agent", userAgent ?? DefaultUserAgent);
            _client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.6,en;q=0.4,es;q=0.2");
            _client.DefaultRequestHeaders.Add("Accept", "*/*");
            _client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        }

        public async Task<StoreScrapeResult> ScrapeAsync(string url, bool downloadImages = false)
        {
            var scraper = GetScraper(url);
            var id = scraper?.GetIdFromUrl(url);
            return await ScrapeAsync(scraper?.Store ?? ScraperStoreType.Unknown, id, downloadImages);
        }
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


        public IStoreScraper GetScraper(string url)
        {
            var type = GetStoreType(url);
            if (type == ScraperStoreType.Unknown)
                return null;
            return GetScraper(type);

        }
        public IStoreScraper GetScraper(ScraperStoreType store)
        {
            switch (store)
            {
                case ScraperStoreType.PlayStore: return new PlayStoreScraper(_client);
                case ScraperStoreType.AppleStore: return new AppleStoreScraper(_client);
                case ScraperStoreType.WindowsStore: return new WindowsStoreScraper(_client);
                default: return null;
            }
        }

        /// <summary>
        /// Checks if URL is valid for any of supported scrapers (whithout making request to app store)
        /// </summary>
        /// <param name="url">URL to parse</param>
        /// <returns>Normalized URL and AppId; Null if URL is invalid</returns>
        public AppIdentification ParseUrl(string url)
        {
            var store = GetStoreType(url);
            var scraper = GetScraper(store);
            var id = scraper?.GetIdFromUrl(url);
            if (string.IsNullOrEmpty(id))
                return null;
            return new AppIdentification() { Id = id, AppUrl = scraper.GetUrlFromId(id), StoreType = store };
        }

        public string GetNormalizedUrl(ScraperStoreType store, string appId)
        {
            var scraper = GetScraper(store);
            return scraper?.GetUrlFromId(appId);
        }



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