using System;
using System.Collections.Generic;

namespace AppStoresScraper
{

    public class AppIdentification
    {
        /// <summary>
        /// Normalized store URL (this URL is user for actual request to store)
        /// </summary>
        public string AppUrl { get; set; }
        /// <summary>
        /// Unique app id in store (this id is set after parsing store)
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// Parsed app information
    /// </summary>
    public class AppMetadata : AppIdentification
    {
        public ScraperStoreType StoreType { get; set; }
        public string Name { get; set; }

        public string IconUrl { get; set; }
        public string Publisher { get; set; }
        public string PublisherEmail { get; set; }
        public string Website { get; set; }
        public ICollection<string> Categories { get; set; }
        public decimal? Rating { get; set; }
        public int? RatingCount { get; set; }
        public DateTime? Updated { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public bool? Paid { get; set; }
        private Dictionary<string, string> _otherValues = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> OtherValues => _otherValues;

        public void AddValue(string key, object value)
        {
            string strVal = value?.ToString();
            _otherValues.Add(key, strVal);
        }
    }
}