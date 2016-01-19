using System.Threading.Tasks;

namespace AppStoresScraper
{
    public interface IStoreParser
    {
        StoreType Store { get; }
        string GetIdFromUrl(string url);
        Task<AppMetadata> Parse(string appId);
        Task<AppIcon> DownloadIcon(AppMetadata meta);

    }
}