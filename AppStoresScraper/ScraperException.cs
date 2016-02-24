using System;

namespace AppStoresScraper
{
    public class ScraperException : Exception
    {
        public ScraperStoreType StoreType { get; set; }
        public string AppId { get; set; }
        public string Url { get; set; }

        public int? StatusCode { get; set; }

        public ScraperException(string message, ScraperStoreType storeType, Exception innerException, string appId = null, string url = null, int? statusCode = null) : base(message, innerException)
        {
            StoreType = storeType;
            AppId = appId;
            Url = url;
            StatusCode = statusCode;
        }

        public ScraperException(string message, ScraperStoreType storeType, string appId = null, string url = null, int? statusCode = null) : this (message, storeType, null, appId, url, statusCode)
        {
            
        }
    }
}