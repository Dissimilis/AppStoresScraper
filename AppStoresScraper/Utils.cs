using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppStoresScraper
{
    public static class Utils
    {
        private static Lazy<Regex> _htmlStripRegex = new Lazy<Regex>(() => new Regex(@"<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase));
        private static Lazy<Regex> _htmlNewLineRegex = new Lazy<Regex>(() => new Regex(@"</?[brph0-9]{1,2}[\s/]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase));
        private static Lazy<Regex> _htmlNewLineCompressionRegex = new Lazy<Regex>(() => new Regex("[\r\n]+", RegexOptions.Compiled));

        /// <summary>
        /// Removes basis HTML tags; Converts br, p and h tags to new lines
        /// </summary>
        /// <param name="removeRepeatingLines"></param>
        /// <returns>String without HTML tags</returns>
        public static string StripHtml(dynamic content, bool removeRepeatingLines = true)
        {
            if (content == null)
                return null;
            var contentStr = content.ToString();
            if (string.IsNullOrEmpty(contentStr))
                return contentStr;
            contentStr = WebUtility.HtmlDecode(contentStr);
            contentStr = _htmlNewLineRegex.Value.Replace(contentStr, Environment.NewLine);
            contentStr = _htmlStripRegex.Value.Replace(contentStr, string.Empty);
            if (removeRepeatingLines)
                contentStr = _htmlNewLineCompressionRegex.Value.Replace(contentStr, Environment.NewLine);
            return contentStr.Trim();
        }
    }
}
