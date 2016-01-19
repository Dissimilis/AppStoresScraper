using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppStoresScraper
{
    public class PlayStoreScraper : IStoreScraper
    {
        private const string IdFromUrlRegex = @"((http.*?://play\.google\.com/store/apps/)|(market://))details\?id=([\w\.]+)";
        private const string IconImgRegex = @"cover-container.+?<img[^>]+?cover-image""[^>]+?src.+?([^""]+)";
        private const string AppNameRegex = "\"id-app-title\".+?>(.+?)</div>";
        private const string DatePublishedRegex = "datePublished.+?>(.+?)<";
        private const string VersionRegex = "softwareVersion.+?>\\s*(.+?)\\s*<";
        private const string PublisherRegex = "Offered By.+?content.+?>(.+?)<";
        private const string PublisherEmailRegex = "dev-link.+?mailto:(.+?)\"";
        private const string WebsiteRegex = "dev-link.+?http.+?=(http?.://.+?)&.+?website";
        private const string CategoriesRegex = @"document-subtitle category.+?/store/apps/category/([\w_]+)";
        private const string PriceRegex = "price buy.+?<span>([^<>]+?)</span>";
        private const string RatingValueRegex = "<meta.+?content=\"([\\d\\.]+)\".+?itemprop=\"ratingValue\"";
        private const string RatingCountRegex = @"<meta.+?content=""([\d\.]+)"" itemprop=""ratingCount""";
        private const string DescriptionRegex = "details-section-contents.+?text-body.+?itemprop.?=.?\"description\".+?<div.+?>(.+?)</div>";
        private const string StoreUrlTemplate = "https://play.google.com/store/apps/details?id={0}&hl=en";


        private static Lazy<Regex> htmlStripRegex = new Lazy<Regex>(() => new Regex(@"<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase));
        private static Lazy<Regex> htmlNewLineRegex = new Lazy<Regex>(() => new Regex(@"<[brph]{1,2}[\s/]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase));
        private HttpClient _client;

        public StoreType Store { get; } = StoreType.PlayStore;
        public PlayStoreScraper(HttpClient client)
        {
            _client = client;
        }

        public string GetIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException(nameof(url));
            return RegexUtils.GetGroup(IdFromUrlRegex, url, 4);
        }
        public async Task<AppMetadata> Scrape(string appId)
        {
            var uri = new Uri(string.Format(StoreUrlTemplate, appId));

            var msg = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await _client.SendAsync(msg);
            var content = await response.Content.ReadAsStringAsync();
            var meta = new AppMetadata() { Id = appId, StoreUrl = uri.AbsoluteUri };
            meta.Name = AppNameRegex.GetGroup(content);
            meta.IconUrl = IconImgRegex.GetGroup(content);
            if (meta.IconUrl != null)
                meta.IconUrl = Regex.Replace(meta.IconUrl, "(.+?=)w\\d\\d\\d", "$1w900");

            if (!string.IsNullOrEmpty(meta.IconUrl) && meta.IconUrl.StartsWith("//"))
                meta.IconUrl = "http:" + meta.IconUrl;
            DateTime datePublished;
            if (DateTime.TryParse(DatePublishedRegex.GetGroup(content), out datePublished))
                meta.Updated = datePublished;
            meta.Version = VersionRegex.GetGroup(content);
            meta.Publisher = StripHtml(PublisherRegex.GetGroup(content));
            meta.PublisherEmail = PublisherEmailRegex.GetGroup(content);
            meta.Website = StripHtml(WebsiteRegex.GetGroup(content));
            meta.Description = StripHtml(DescriptionRegex.GetGroup(content));
            decimal rating;
            if (decimal.TryParse(RatingValueRegex.GetGroup(content), NumberStyles.Any, CultureInfo.InvariantCulture, out rating))
                meta.Rating = rating;
            int ratingCount;
            if (int.TryParse(RatingCountRegex.GetGroup(content), out ratingCount))
                meta.RatingCount = ratingCount;
            string price = PriceRegex.GetGroup(content);
            if (price != null)
                meta.Paid = Regex.IsMatch(price, "[\\d]");
            meta.AddValue("Price", price);
            meta.Categories = CategoriesRegex.GetGroupMany(content, 1).ToList();

            return meta;
        }
        public async Task<AppIcon> DownloadIcon(AppMetadata meta)
        {
            if (string.IsNullOrEmpty(meta.IconUrl))
                throw new ArgumentException("Metadata has empty icon url", nameof(meta));
            var result = new AppIcon();
            var msg = new HttpRequestMessage(HttpMethod.Get, meta.IconUrl);
            var httpResult = await _client.SendAsync(msg);
            result.Format = httpResult.Content?.Headers?.FirstOrDefault(c => c.Key == "Content-Type").Value?.FirstOrDefault();
            result.Content = await httpResult.Content.ReadAsByteArrayAsync();
            return result;
        }

        private string StripHtml(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
            content = WebUtility.HtmlDecode(content);
            content = htmlNewLineRegex.Value.Replace(content, "\r\n");
            content = htmlStripRegex.Value.Replace(content, string.Empty);
            return content.Trim();
        }
    }
}