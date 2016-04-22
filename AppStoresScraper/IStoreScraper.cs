using System.Threading.Tasks;

namespace AppStoresScraper
{
    public interface IStoreScraper
    {
        ScraperStoreType Store { get; }
        string GetIdFromUrl(string url);
        string GetUrlFromId(string id);
        Task<AppMetadata> ScrapeAsync(string appId);
        Task<AppIcon> DownloadIconAsync(AppMetadata meta);

    }
}