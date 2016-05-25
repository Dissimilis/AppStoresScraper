using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AppStoresScraper
{
    public interface IStoreScraper
    {
        string GetIdFromUrl(string url);
        string GetUrlFromId(string id);

        /// <summary>
        /// Opens store page and parses results
        /// </summary>
        /// <param name="appId">ID of app to scrape</param>
        /// <returns>Should return null only when app not found in store or throw exception otherwise</returns>
        Task<AppMetadata> ScrapeAsync(string appId);
        Task<AppIcon> DownloadIconAsync(AppMetadata meta);

        Action<TraceLevel, string, Exception> LogWritter { get; set; }

    }
}