using Westwind.Globalization;

namespace KeplerCMS.Helpers
{
    public static class Texts
    {
        /// <summary>
        /// A cms_localizations text, or the fallback while it's missing (DbRes then returns the key, or an invariant row
        /// whose value is the key, which it adds by itself).
        /// </summary>
        public static string T(string key, string set, string fallback)
        {
            var value = DbRes.T(key, set);
            return string.IsNullOrWhiteSpace(value) || value == key ? fallback : value;
        }
    }
}
