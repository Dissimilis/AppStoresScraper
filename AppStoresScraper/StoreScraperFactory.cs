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

        public async Task<StoreScrapeResult> Scrape(string url, bool downloadImages)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new StoreScrapeResult();
            try
            {
                var screaper = GetScraper(url);
                if (screaper == null)
                    return result;
                result.Store = screaper.Store;
                var id = screaper.GetIdFromUrl(url);
                result.AppId = id;
                if (string.IsNullOrEmpty(id))
                    return result;
                result.Metadata = await screaper.Scrape(id);
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
            if (type == null)
                return null;
            return GetScraper(type.Value);

        }
        public IStoreScraper GetScraper(StoreType store)
        {
            switch (store)
            {
                case StoreType.PlayStore: return new PlayStoreScraper(_client);
                case StoreType.ITunes: return new TunesStoreScraper(_client);
                case StoreType.WindowsStore: return new WindowsStoreScraper(_client);
                default: return null;
            }
        }

        public StoreType? GetStoreType(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;
            if (url.StartsWith("https://play.google.com/store/", StringComparison.OrdinalIgnoreCase) || url.StartsWith("market://details?id=", StringComparison.OrdinalIgnoreCase))
                return StoreType.PlayStore;
            if (url.StartsWith("https://itunes.apple.com/", StringComparison.OrdinalIgnoreCase))
                return StoreType.ITunes;
            if (Regex.IsMatch(url, @"http.*?://w*?\.*?microsoft.com/.*?/?store/.+", RegexOptions.IgnoreCase))
                return StoreType.WindowsStore;
            return null;
        }
    }
}