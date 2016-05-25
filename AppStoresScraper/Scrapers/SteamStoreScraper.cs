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
    public class SteamStoreScraper : StoreScraperBase
    {
        protected const string StoreUrlTemplate = "http://store.steampowered.com/api/appdetails?appids={0}";
        protected const string StoreUrlTemplate2 = "http://store.steampowered.com/app/{0}/";
        protected const string DescriptionRegex = "<meta.+?\"description\".+?content\\s?=\\s?\"(.+?)\"";
        protected HttpClient _client;


        protected override string StoreUrlUserTemplate { get; } = "http://store.steampowered.com/app/{0}/";
        protected override string IdFromUrlRegex { get; } = @"http.*?://store\.steampowered\.com/app/([\d\.]+)";

        public string GetIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException(nameof(url));
            return IdFromUrlRegex.GetGroup(url);
        }
        public SteamStoreScraper(HttpClient client)
        {
            _client = client;
        }

        public override async Task<AppMetadata> ScrapeAsync(string appId)
        {
            var url = string.Format(StoreUrlTemplate, appId);
            var url2 = string.Format(StoreUrlTemplate2, appId);
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Add("Accept", "text/json");
            var response = await _client.SendAsync(msg);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var json = JsonConvert.DeserializeObject<dynamic>(content);
            if (json == null || json[appId]["success"] != true)
            {
                LogWritter?.Invoke(TraceLevel.Warning, $"Steam url [{url}] returned unsuccessfull status (app not found)", null);
                return null;
            }
            
            dynamic result = json[appId].data;
            var meta = new AppMetadata() { Id = result.steam_appid, ScraperType = this.GetType(), AppUrl = GetUrlFromId(appId) };

            var response2 = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url2));
            if (!response2.IsSuccessStatusCode)
            {
                LogWritter?.Invoke(TraceLevel.Warning, $"Steam url [{url2}] returned status code [{response2.StatusCode}]; This URL is only used for DescriptionShort;", null);
            }
            else
            {
                var content2 = await response2.Content.ReadAsStringAsync();
                meta.AddValue("DescriptionShort", DescriptionRegex.GetGroup(content2)?.Trim());
            }

            meta.Name = result.name;
            meta.IconUrl = result.header_image;
            meta.Publisher = result.publishers[0]?.ToString();
            meta.Website = result.website ?? result.support_info?.url;
            meta.PublisherEmail = result.support_info.email;
            meta.Categories = ArrayToList(result.genres, "description");
            meta.Rating = result.metacritic?.score;
            meta.Description = Utils.StripHtml(result.about_the_game);
            meta.Paid = result.is_free == false;
            
            meta.AddValue("DescriptionLong", Utils.StripHtml(result.detailed_description));
            meta.AddValue("DescriptionLongRaw", result.detailed_description?.ToString());
            meta.AddValue("Price", result.price_overview?.initial/100m);
            meta.AddValue("PriceCurrency", result.price_overview?.currency?.ToString());
            meta.AddValue("SupportedLanguages", Utils.StripHtml(result.supported_languages));
            meta.AddValue("RequiredAge", result.required_age?.ToString());
            meta.AddValue("Type", result.type?.ToString());
            meta.AddValue("AboutGameRaw", result.about_the_game?.ToString());
            meta.AddValue("LegalNotice", result.legal_notice?.ToString());
            meta.AddValue("Developers", ArrayToList(result.developers));
            meta.AddValue("Publishers", ArrayToList(result.publishers));
            meta.AddValue("Categories", ArrayToList(result.categories, "description"));
            meta.AddValue("Screenshots", ArrayToList(result.screenshots, "path_full"));
            meta.AddValue("PlatformWindows", result.platforms?.windows);
            meta.AddValue("PlatformLinux", result.platforms?.linux);
            meta.AddValue("PlatformMac", result.platforms?.mac);
            meta.AddValue("ReleaseDate", result.release_date?.date);
            meta.AddValue("Background", result.background);

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
        private IList<string> ArrayToList(JArray array, string propName)
        {
            return array?.Select(x => x[propName]?.ToString()).Distinct().ToList();
        }
        private IList<string> ArrayToList(JArray array)
        {
            return array?.Select(x => x?.ToString()).Distinct().ToList();
        }
    }
}