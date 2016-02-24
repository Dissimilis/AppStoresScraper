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

        public async Task<StoreScrapeResult> ScrapeAsync(string url, bool downloadImages)
        {
            var scraper = GetScraper(url);
            var id = scraper?.GetIdFromUrl(url);
            return await ScrapeAsync(scraper?.Store ?? ScraperStoreType.Unknown, id, downloadImages);
        }
        public async Task<StoreScrapeResult> ScrapeAsync(ScraperStoreType type, string appId, bool downloadImages)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new StoreScrapeResult();
            try
            {
                var screaper = GetScraper(type);
                if (screaper == null)
                    return result;
                result.Store = screaper.Store;
                result.AppId = appId;
                if (string.IsNullOrEmpty(appId))
                    return result;
                result.Metadata = await screaper.ScrapeAsync(appId);
                result.Metadata.StoreType = result.Store;
                if (result.Metadata?.IconUrl != null && result.Metadata.IconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    result.Icon = await screaper.DownloadIcon(result.Metadata);
                }
            }
            catch (Exception ex)
            {
                var wex = ex as WebException;
                if (wex != null)
                {
                    result.ResponseErrorStatusCode = (int)(wex.Response as HttpWebResponse)?.StatusCode;
                }
                result.Exception = ex;
            }
            finally
            {
                result.ParseTime = sw.Elapsed;
            }
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
                case ScraperStoreType.ITunes: return new TunesStoreScraper(_client);
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
            return new AppIdentification() { Id = id, AppUrl = scraper.GetUrlFromId(id) };
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
                return ScraperStoreType.ITunes;
            if (Regex.IsMatch(url, @"http.*?://w*?\.*?microsoft.com/.*?/?store/.+", RegexOptions.IgnoreCase))
                return ScraperStoreType.WindowsStore;
            return ScraperStoreType.Unknown;
        }
    }
}