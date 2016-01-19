using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppStoresScraper
{
    public class TunesStoreScraper : IStoreScraper
    {
        private const string IdFromUrlRegex = @"http.*?://w*?\.*?itunes\.apple\.com/[\w]*?/app/[\w-]*?/id([\d]+)";
        private const string StoreUrlTemplate = "http://itunes.apple.com/lookup?id={0}";
        private HttpClient _client;

        public StoreType Store { get; } = StoreType.ITunes;
        public string UserAgent { get; set; }


        public string GetIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException(nameof(url));
            return RegexUtils.GetGroup(IdFromUrlRegex, url);
        }
        public TunesStoreScraper(HttpClient client)
        {
            _client = client;
        }

        public async Task<AppMetadata> Scrape(string appId)
        {
            var url = string.Format(StoreUrlTemplate, appId);
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Add("Accept", "text/json");
            var response = await _client.SendAsync(msg);
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<dynamic>(content);
            var result = json["results"][0];
            var meta = new AppMetadata() { Id = result.trackId };
            meta.StoreUrl = result.trackViewUrl;
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
            meta.AddValue("Price", result.formattedPrice.ToString());
            meta.AddValue("artistId", result.artistId);
            meta.AddValue("primaryGenreName", result.primaryGenreName);
            meta.AddValue("releaseNotes", result.releaseNotes);
            meta.AddValue("trackCensoredName", result.trackCensoredName);
            meta.AddValue("fileSizeBytes", result.fileSizeBytes);
            meta.AddValue("trackId", result.trackId);
            meta.AddValue("kind", result.formattedPrice);
            meta.AddValue("bundleId", result.bundleId);
            DateTime date = DateTime.Now;
            if (DateTime.TryParse((result.currentVersionReleaseDate ?? string.Empty).ToString(), out date))
            {
                meta.Updated = date;
            }
            return meta;

        }
        public async Task<AppIcon> DownloadIcon(AppMetadata meta)
        {
            if (string.IsNullOrEmpty(meta.IconUrl))
                throw new ArgumentException("Metadata has empty icon url", nameof(meta));
            var result = new AppIcon();
            var httpResult = await _client.GetAsync(new Uri(meta.IconUrl));
            result.Format = httpResult.Content?.Headers?.FirstOrDefault(c => c.Key == "Content-Type").Value?.FirstOrDefault();
            result.Content = await httpResult.Content.ReadAsByteArrayAsync();
            return result;
        }
        private IList<string> ArrayToList(JArray array)
        {
            if (array == null)
                return null;
            return (array).Select(x => x.ToString()).Distinct().ToList();
        }
    }
}