using System;

namespace AppStoresScraper
{
    public class ScraperException : Exception
    {
        public Type ScraperType { get; set; }
        public string AppId { get; set; }
        public string Url { get; set; }

        public int? StatusCode { get; set; }

        public ScraperException(string message, IStoreScraper scraper, Exception innerException, string appId = null, string url = null, int? statusCode = null) : base(message, innerException)
        {
            ScraperType = scraper.GetType();
            AppId = appId;
            Url = url;
            StatusCode = statusCode;
        }

        public ScraperException(string message, IStoreScraper scraper, string appId = null, string url = null, int? statusCode = null) : this (message, scraper, null, appId, url, statusCode)
        {
            
        }
    }
}