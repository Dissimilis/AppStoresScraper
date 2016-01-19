using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppStoresScraper
{
    public class WindowsStoreParser : IStoreParser
    {
        private const string IdFromUrlRegex = @"http.*?://w*?\.*?microsoft.com/[\w-/]*?store/.+/([\w]+)";//https://www.microsoft.com/store/apps/{0}
        private const string StoreUrlTemplate = "https://storeedgefd.dsx.mp.microsoft.com/pages/pdp?productId={0}&appVersion=2015.9.9.2&market=US&locale=en-US&deviceFamily=Windows.Desktop";
        private const string StoreUrlUserTemplate = "https://www.microsoft.com/store/apps/{0}";
        private HttpClient _client;

        public StoreType Store { get; } = StoreType.WindowsStore;

        //Uses this color if API returns transparent or empty BgColor

        public string DefaultBgColor { get; set; } = "#2f5ef6";

        public string GetIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException(nameof(url));
            return RegexUtils.GetGroup(IdFromUrlRegex, url);
        }
        public WindowsStoreParser(HttpClient client)
        {
            _client = client;
        }

        public async Task<AppMetadata> Parse(string appId)
        {
            var url = string.Format(StoreUrlTemplate, appId);
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Add("MS-Contract-Version", "4");
            var response = await _client.SendAsync(msg);
            var content = await response.Content.ReadAsStringAsync();
            var result = GetAppPayload(content, appId);
            var meta = new AppMetadata() { Id = result.ProductId };
            meta.StoreUrl = string.Format(StoreUrlUserTemplate, meta.Id);
            meta.Name = result.Title;
            var icon = GetIcon(result.Images);
            meta.IconUrl = icon?.Url;
            meta.PublisherEmail = GetFirst<string>(result.SupportUris, "Uri", "mailto:", "Uri")?.Replace("mailto:", string.Empty);
            meta.Publisher = result.PublisherName;
            meta.Website = result.AppWebsiteUrl;
            meta.Categories = ArrayToList(result.Categories);
            meta.Rating = result.AverageRating;
            meta.RatingCount = result.RatingCount;
            meta.Description = result.Description;
            meta.Version = result.Version;
            meta.Paid = result.Price > 0;
            meta.AddValue("Price", result.DisplayPrice);
            meta.AddValue("IsUniversal", result.IsUniversal);
            meta.AddValue("BgColor", icon?.BackgroundColor ?? result.BGColor);
            meta.AddValue("HasFreeTrial", result.HasFreeTrial);
            meta.AddValue("ProductType", result.ProductType);
            meta.AddValue("PackageFamilyName", result.PackageFamilyName);
            meta.AddValue("CategoryId", result.CategoryId);
            meta.AddValue("ApproximateSizeInBytes", result.ApproximateSizeInBytes);
            meta.AddValue("SubcategoryId", result.SubcategoryId);
            meta.AddValue("Language", result.Language);
            meta.AddValue("ImageType", icon.ImageType);
            DateTime date = DateTime.Now;
            if (DateTime.TryParse((result.LastUpdateDateUtc ?? string.Empty).ToString(), out date))
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

            if (!result.Format.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                var imgStream = await httpResult.Content.ReadAsStreamAsync();
                var color = GetBgColor(meta);
                result.Content = AddBackgroundColor(color, imgStream);
                meta.AddValue("BgColorUsed", color);
            }
            else
            {
                result.Content = await httpResult.Content.ReadAsByteArrayAsync();
            }
            return result;
        }
        private Color GetBgColor(AppMetadata meta)
        {
            try
            {
                string hexColor;
                if (meta.OtherValues.TryGetValue("BgColor", out hexColor) && hexColor != null)
                {
                    if (string.IsNullOrWhiteSpace(hexColor))
                        hexColor = DefaultBgColor;
                    var color = System.Drawing.ColorTranslator.FromHtml(hexColor);
                    if (color.A <= 0)
                        color = System.Drawing.ColorTranslator.FromHtml(DefaultBgColor);
                    return color;
                }
            }
            catch { }
            return Color.Black;
        }
        private byte[] AddBackgroundColor(Color color, Stream imageStream)
        {
            Bitmap bmp = new Bitmap(imageStream);
            using (var b = new Bitmap(bmp.Width, bmp.Height))
            {
                b.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

                using (var g = Graphics.FromImage(b))
                {
                    g.Clear(color);
                    g.DrawImageUnscaled(bmp, 0, 0);
                }
                using (var ms = new MemoryStream())
                {
                    b.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }


        private dynamic GetAppPayload(string content, string appId)
        {
            var json = JsonConvert.DeserializeObject<dynamic>(content);
            var payload = GetFirst<dynamic>((JArray)json, "Path", "/channels/products/", "Payload");
            return payload;
        }
        private T GetFirst<T>(JArray array, string propToFilterBy, string startsWith, string propToGet) where T : class
        {
            if (array == null)
                return default(T);
            var prop = array?.FirstOrDefault(o => o?.Value<string>(propToFilterBy)?.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase) == true)?.Value<T>(propToGet);
            return prop;
        }
        private Image GetIcon(JArray imgs)
        {
            var images = imgs.ToObject<List<Image>>();
            var filtered = images.Where(i => i.IsIcon).OrderBy(i => i.Order).ThenByDescending(i => i.Width).FirstOrDefault();
            return filtered;
        }

        private IList<string> ArrayToList(JArray array)
        {
            if (array == null)
                return null;
            return (array).Select(x => x.ToString()).Distinct().ToList();
        }

        private class Image
        {
            public string Url { get; set; }
            public string ImageType { get; set; }
            public string BackgroundColor { get; set; }
            public int Height { get; set; }
            public int Width { get; set; }
            public int Order
            {
                get
                {
                    if (ImageType == null)
                        return 99;
                    if (ImageType.Equals("tile", StringComparison.OrdinalIgnoreCase))
                        return 1;
                    if (ImageType.Equals("logo", StringComparison.OrdinalIgnoreCase))
                        return 1;
                    if (ImageType.Equals("hero", StringComparison.OrdinalIgnoreCase))
                        return 2;
                    return 99;
                }
            }
            public bool IsIcon
            {
                get
                {
                    return ImageType != null && !string.IsNullOrEmpty(Url) && Width == Height;
                }
            }

        }
    }
}