using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppStoresScraper
{
    public class StoreParserFactory
    {
        public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; LongBeach/1.0)";

        private HttpClient _client;
        public StoreParserFactory(string userAgent = null)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Add("User-Agent", userAgent ?? DefaultUserAgent);
            _client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.6,en;q=0.4,es;q=0.2");
            _client.DefaultRequestHeaders.Add("Accept", "*/*");
            _client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        }

        public async Task<StoreParseResult> Parse(string url, bool downloadImages)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = new StoreParseResult();
            try
            {
                var parser = GetParser(url);
                if (parser == null)
                    return result;
                result.Store = parser.Store;
                var id = parser.GetIdFromUrl(url);
                result.AppId = id;
                if (string.IsNullOrEmpty(id))
                    return result;
                result.Metadata = await parser.Parse(id);
                if (result.Metadata?.IconUrl != null && result.Metadata.IconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    result.Icon = await parser.DownloadIcon(result.Metadata);
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

        public IStoreParser GetParser(string url)
        {
            var type = GetStoreType(url);
            if (type == null)
                return null;
            return GetParser(type.Value);

        }
        public IStoreParser GetParser(StoreType store)
        {
            switch (store)
            {
                case StoreType.PlayStore: return new PlayStoreParser(_client);
                case StoreType.ITunes: return new ITunesStoreParser(_client);
                case StoreType.WindowsStore: return new WindowsStoreParser(_client);
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