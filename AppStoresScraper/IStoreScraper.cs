using System.Threading.Tasks;

namespace AppStoresScraper
{
    public interface IStoreScraper
    {
        StoreType Store { get; }
        string GetIdFromUrl(string url);
        Task<AppMetadata> Scrape(string appId);
        Task<AppIcon> DownloadIcon(AppMetadata meta);

    }
}