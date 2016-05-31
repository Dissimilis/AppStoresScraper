using System;

namespace AppStoresScraper
{
    public class StoreScrapeResult
    {
        public AppMetadata Metadata { get; set; }
        public Type ScraperType { get; set; }
        public AppIcon Icon { get; set; }
        public string AppId { get; set; }

        public TimeSpan ParseTime { get; set; }
        public ScraperException Exception { get; set; }
        public bool IsSuccessful => Exception == null && ScraperType != null && Metadata != null;
    }
}
