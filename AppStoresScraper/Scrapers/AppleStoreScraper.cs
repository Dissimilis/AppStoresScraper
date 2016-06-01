using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppStoresScraper
{
    public class AppleStoreScraper : StoreScraperBase
    {
        protected const string StoreUrlTemplate = "http://itunes.apple.com/lookup?id={0}";
        protected HttpClient _client;

        protected override string StoreUrlUserTemplate { get; } = "https://itunes.apple.com/app/id{0}";
        protected override string IdFromUrlRegex { get; } = @"^http.*?://w*?\.*?itunes\.apple\.com/.*/?app/.*?/?id([\d]+)";


        public AppleStoreScraper(HttpClient client)
        {
            _client = client;
        }


        public override async Task<AppMetadata> ScrapeAsync(string appId)
        {
            var url = string.Format(StoreUrlTemplate, appId);
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Add("Accept", "text/json");
            var response = await _client.SendAsync(msg);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<dynamic>(content);
            if (json?.resultCount == 0)
            {
                LogWritter?.Invoke(TraceLevel.Warning, $"Apple store (ITunes) url [{url}] returned zero results", null);
                return null;
            }
            var result = json["results"][0];
            var meta = new AppMetadata() { Id = result.trackId, ScraperType = this.GetType(), AppUrl = GetUrlFromId(appId) };
            meta.Name = result.trackName;
            meta.IconUrl = result.artworkUrl512 ?? result.artworkUrl100 ?? result.artworkUrl60;
            meta.Publisher = result.sellerName ?? result.artistName;
            meta.Website = result.sellerUrl;
            meta.Categories = ArrayToList(result.genres);
            meta.Rating = result.averageUserRating;
            meta.RatingCount = result.userRatingCount;
            meta.Description = result.description;
            meta.Version = result.version;
            meta.Paid = result.price > 0;
            meta.AddValue("Url", result.trackViewUrl.ToString());
            meta.AddValue("Price", result.formattedPrice.ToString());
            meta.AddValue("artistId", result.artistId);
            meta.AddValue("primaryGenreName", result.primaryGenreName);
            meta.AddValue("releaseNotes", result.releaseNotes);
            meta.AddValue("trackCensoredName", result.trackCensoredName);
            meta.AddValue("fileSizeBytes", result.fileSizeBytes);
            meta.AddValue("trackId", result.trackId);
            meta.AddValue("kind", result.formattedPrice);
            meta.AddValue("bundleId", result.bundleId);
            DateTime date;
            if (DateTime.TryParse((result.currentVersionReleaseDate ?? string.Empty).ToString(), out date))
            {
                meta.Updated = date;
            }
            return meta;
        }


        public override async Task<AppIcon> DownloadIconAsync(AppMetadata meta)
        {
            if (string.IsNullOrEmpty(meta.IconUrl))
                throw new ArgumentException("Metadata has empty icon url", nameof(meta));
            var result = new AppIcon();
            var httpResult = await _client.GetAsync(new Uri(meta.IconUrl));
            if (httpResult.Content != null)
            {
                result.ContentType = httpResult.Content?.Headers?.ContentType?.MediaType;
                result.Content = await httpResult.Content.ReadAsByteArrayAsync();
            }
            return result;
        }
        private IList<string> ArrayToList(JArray array)
        {
            return array?.Select(x => x.ToString()).Distinct().ToList();
        }
    }
}