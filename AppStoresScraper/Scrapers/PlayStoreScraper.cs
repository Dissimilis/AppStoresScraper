using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppStoresScraper
{
    public class PlayStoreScraper : StoreScraperBase
    {
        protected const string IconImgRegex = @"cover-container.+?<img[^>]+?cover-image""[^>]+?src.+?([^""]+)";
        protected const string AppNameRegex = "\"id-app-title\".+?>(.+?)</div>";
        protected const string DatePublishedRegex = "datePublished.+?>(.+?)<";
        protected const string VersionRegex = "softwareVersion.+?>\\s*(.+?)\\s*<";
        protected const string PublisherRegex = "Offered By.+?content.+?>(.+?)<";
        protected const string PublisherEmailRegex = "dev-link.+?mailto:(.+?)\"";
        protected const string WebsiteRegex = "dev-link.+?http.+?=(http?.://.+?)&.+?website";
        protected const string CategoriesRegex = @"document-subtitle category.+?/store/apps/category/([\w_]+)";
        protected const string PriceRegex = "price buy.+?<span>([^<>]+?)</span>";
        protected const string RatingValueRegex = "<meta.+?content=\"([\\d\\.]+)\".+?itemprop=\"ratingValue\"";
        protected const string RatingCountRegex = @"<meta.+?content=""([\d\.]+)"" itemprop=""ratingCount""";
        protected const string DescriptionRegex = "details-section-contents.+?text-body.+?itemprop.?=.?\"description\".+?<div.+?>(.+?)</div>";
        protected HttpClient _client;

        protected override string StoreUrlUserTemplate { get; } = "https://play.google.com/store/apps/details?id={0}&hl=en";
        protected override string IdFromUrlRegex { get; } = @"^((http.*?://play\.google\.com/store/apps/)|(market://))details\?id=([\w\.]+)";

        public PlayStoreScraper(HttpClient client)
        {
            _client = client;
        }

        public override string GetIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException(nameof(url));
            return IdFromUrlRegex.GetGroup(url,4);
        }

        public override async Task<AppMetadata> ScrapeAsync(string appId)
        {
            var url = GetUrlFromId(appId);
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _client.SendAsync(msg);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                LogWritter?.Invoke(TraceLevel.Warning, $"PlayStore url [{url}] returned 404", null);
                return null;
            }
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var meta = new AppMetadata() { Id = appId, AppUrl = url, ScraperType = this.GetType() };
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
            meta.Publisher = Utils.StripHtml(PublisherRegex.GetGroup(content));
            meta.PublisherEmail = PublisherEmailRegex.GetGroup(content);
            meta.Website = Utils.StripHtml(WebsiteRegex.GetGroup(content));
            meta.Description = Utils.StripHtml(DescriptionRegex.GetGroup(content));
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
        public override async Task<AppIcon> DownloadIconAsync(AppMetadata meta)
        {
            if (string.IsNullOrEmpty(meta.IconUrl))
                throw new ArgumentException("Metadata has empty icon url", nameof(meta));
            var result = new AppIcon();
            var msg = new HttpRequestMessage(HttpMethod.Get, meta.IconUrl);
            var httpResult = await _client.SendAsync(msg);
            if (httpResult.Content != null)
            {
                result.ContentType = httpResult.Content?.Headers?.ContentType?.MediaType;
                result.Content = await httpResult.Content.ReadAsByteArrayAsync();
            }
            return result;
        }

        
    }
}