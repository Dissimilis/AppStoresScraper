using System;
using System.Collections.Generic;
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


        protected readonly Dictionary<Type, IStoreScraper> RegisteredScrapers = new Dictionary<Type, IStoreScraper>();


        /// <summary>
        /// HTTP client for making requests
        /// </summary>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// Log writter used to debug internars of scraper
        /// </summary>
        public Action<TraceLevel, string, Exception> LogWritter { get; set; }

        public StoreScraperFactory(HttpClient httpClient = null, string userAgent = DefaultUserAgent)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };

            if (httpClient == null)
            {
                HttpClient = new HttpClient(handler);
                HttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
                HttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.6");
                HttpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                HttpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            }
            else
            {
                HttpClient = httpClient;
            }
            RegisteredScrapers.Add(typeof(WindowsStoreScraper), new WindowsStoreScraper(HttpClient) { LogWritter = LogWritter });
            RegisteredScrapers.Add(typeof(AppleStoreScraper), new AppleStoreScraper(HttpClient) { LogWritter = LogWritter });
            RegisteredScrapers.Add(typeof(PlayStoreScraper), new PlayStoreScraper(HttpClient) { LogWritter = LogWritter });
            RegisteredScrapers.Add(typeof(SteamStoreScraper), new SteamStoreScraper(HttpClient) {LogWritter = LogWritter});
        }

        /// <summary>
        /// Detects store type from URL and gets app metadata
        /// </summary>
        /// <param name="url">URL to app</param>
        /// <param name="downloadImages">If true, downloads app icon from store</param>
        /// <returns>Scraping result. Scraping exception are stored int result.Exception</returns>
        public virtual async Task<StoreScrapeResult> ScrapeAsync(string url, bool downloadImages = false)
        {
            var scraper = GetScraper(url);
            var id = scraper?.GetIdFromUrl(url);
            if (scraper == null)
                LogWritter?.Invoke(TraceLevel.Info, $"No scraper registered for URL [{url}]", null);
            else if (id == null)
                LogWritter?.Invoke(TraceLevel.Info, $"Scraper [{scraper.GetType().Name}] can't get app ID from URL [{url}]", null);
            return await ScrapeAsync(scraper, id, downloadImages);
        }

        /// <summary>
        /// Gets app metadata from provided store
        /// </summary>
        /// <param name="appId">App id in store</param>
        /// <param name="downloadImages">If true, downloads app icon from store</param>
        /// <param name="scraper">App store scraper</param>
        /// <returns>Scraping result. Scraping exception are stored int result.Exception</returns>
        public virtual async Task<StoreScrapeResult> ScrapeAsync(IStoreScraper scraper, string appId, bool downloadImages = true)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new StoreScrapeResult();
            if (scraper == null)
            {
                return result;
            }
            result.ScraperType = scraper.GetType();
            result.AppId = appId;
            if (string.IsNullOrEmpty(appId))
                return result;
            try
            {
                result.Metadata = await scraper.ScrapeAsync(appId);
                result.Metadata.ScraperType = result.ScraperType;
                if (result.Metadata != null)
                {
                    if (downloadImages)
                    {
                        if (result.Metadata.IconUrl != null && result.Metadata.IconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Icon = await scraper.DownloadIconAsync(result.Metadata);
                        }
                        else
                        {
                            LogWritter?.Invoke(TraceLevel.Warning, $"{scraper.GetType().Name} scraper did not found valid icon url [{result.Icon}]", null);
                        }
                    }
                    if (string.IsNullOrWhiteSpace(result.Metadata.Name))
                        result.Exception = new ScraperException(scraper.GetType().Name + " scraper failed to parse result", scraper, appId, result.Metadata.AppUrl);
                }
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
                LogWritter?.Invoke(TraceLevel.Error, $"{scraper.GetType().Name} scraper threw an exception while scraping [{url}]", ex);
                result.Exception = new ScraperException(scraper.GetType().Name + " scraper failed", scraper, ex, appId, url, statusCode);
            }
            result.ParseTime = sw.Elapsed;
            return result;
        }

        /// <summary>
        /// Analyzes URL and returns specific store scraper
        /// </summary>
        /// <param name="url">URL of app</param>
        /// <returns>null when URL is invalid</returns>
        public virtual IStoreScraper GetScraper(string url)
        {
            if (string.IsNullOrEmpty(url) || RegisteredScrapers == null)
                return null;
            foreach (var s in RegisteredScrapers)
            {
                if (!string.IsNullOrWhiteSpace(s.Value?.GetIdFromUrl(url)))
                {
                    return s.Value;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Gets registered scraper instance
        /// </summary>
        /// <typeparam name="T">Type of store scraper</typeparam>
        /// <returns>Scraper instance</returns>
        public virtual IStoreScraper GetScraper<T>() where T:IStoreScraper
        {
            IStoreScraper scraper = null;
            RegisteredScrapers.TryGetValue(typeof(T), out scraper);
            if (scraper == null)
                LogWritter?.Invoke(TraceLevel.Warning, $"Scraper {typeof(T).Name} is not registered", null);
            return scraper;
        }

        /// <summary>
        /// Checks if URL is valid for any of supported scrapers (whithout making request to app store)
        /// </summary>
        /// <param name="url">URL to parse</param>
        /// <returns>Normalized URL and AppId; null if URL is invalid</returns>
        public virtual AppIdentification ParseUrl(string url)
        {
            var scraper = GetScraper(url);
            var id = scraper?.GetIdFromUrl(url);
            if (string.IsNullOrEmpty(id))
                return null;
            return new AppIdentification() { Id = id, AppUrl = scraper.GetUrlFromId(id), ScraperType = scraper.GetType() };
        }

        /// <summary>
        /// Constructs standard deterministic URL to app store from store type and app id in that store
        /// </summary>
        /// <param name="store">Store type</param>
        /// <param name="appId">App id in store</param>
        /// <returns>Full URL to app (ignores locality)</returns>
        public virtual string GetNormalizedUrl<T>(string appId) where T: IStoreScraper
        {
            var scraper = GetScraper<T>();
            return scraper?.GetUrlFromId(appId);
        }

    }
}