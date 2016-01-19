using System;
using System.Collections.Generic;

namespace AppStoresScraper
{
    public class AppMetadata
    {
        public string Name { get; set; }
        public string StoreUrl { get; set; }
        public string IconUrl { get; set; }
        public string Id { get; set; }
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
        public IReadOnlyDictionary<string, string> OtherValues { get { return _otherValues; } }

        public void AddValue(string key, object value)
        {
            string strVal = value != null ? value.ToString() : null;
            _otherValues.Add(key, strVal);
        }

    }
}