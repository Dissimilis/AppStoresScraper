using System;

namespace AppStoresScraper
{
    public class StoreScrapeResult
    {
        public AppMetadata Metadata { get; set; }
        public ScraperStoreType Store { get; set; }
        public AppIcon Icon { get; set; }
        public string AppId { get; set; }

        public TimeSpan ParseTime { get; set; }
        public int ResponseErrorStatusCode { get; set; }
        public Exception Exception { get; set; }
        public bool IsSuccessful => Exception == null && Store != ScraperStoreType.Unknown;
    }
}
