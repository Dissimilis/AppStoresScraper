using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AppStoresScraper
{
    public abstract class StoreScraperBase : IStoreScraper
    {
        protected abstract string IdFromUrlRegex { get; }

        public Action<TraceLevel, string, Exception> LogWritter { get; set; }
        /// <summary>
        /// Template of user facing store url
        /// </summary>
        protected abstract string StoreUrlUserTemplate { get; }

        public virtual string GetIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException(nameof(url));
            return IdFromUrlRegex.GetGroup(url);
        }

        public virtual string GetUrlFromId(string appId)
        {
            if (appId == null)
                throw new ArgumentNullException(nameof(appId));
            return string.Format(StoreUrlUserTemplate, appId);
        }

        public abstract Task<AppMetadata> ScrapeAsync(string appId);

        public abstract Task<AppIcon> DownloadIconAsync(AppMetadata meta);
        public virtual Task<AppIcon[]> DownloadIconsAsync(AppMetadata meta)
        {
            return Task.FromResult(new [] { DownloadIconAsync(meta).Result });
        }
        
    }
}