using Narochno.BBCode;
using System.Text.RegularExpressions;

namespace KeplerCMS.Areas.MyHabbo.Helpers
{
    /// <summary>
    /// Renders the BBCode of notes and guestbook entries as HTML. The parser HTML-encodes text and attribute
    /// values; attribute values that end up in a URL or in script are checked here first.
    /// </summary>
    public static class BBCodeToString
    {
        // [url=...] only to http(s) or a site-relative path: no javascript:, data: and the like.
        private static readonly Regex UrlTag = new Regex(@"\[url=([^\]]*)\]", RegexOptions.IgnoreCase);
        private static readonly Regex SafeUrl = new Regex(@"^(https?://|/(?!/))[^\s""'<>]*$", RegexOptions.IgnoreCase);

        // [room=...], [habbo=...] and [group=...] take a numeric id; the room id goes into an onclick handler.
        private static readonly Regex IdTag = new Regex(@"\[(room|habbo|group)=([^\]]*)\]", RegexOptions.IgnoreCase);

        private static readonly BBCodeParser Parser = new BBCodeParser(new[]
        {
            new BBTag("b", "<b>", "</b>"),
            new BBTag("i", "<i>", "</i>"),
            new BBTag("u", "<u>", "</u>"),
            new BBTag("code", "<pre>", "</pre>"),
            new BBTag("quote", "<div class=\"bbcode-quote\">", "</div>"),
            new BBTag("color", "<font color=\"${color}\">", "</font>", new BBAttribute("color", ""), new BBAttribute("color", "color")),
            new BBTag("habbo", "<a href=\"/home/${id}/id\">", "</a>", new BBAttribute("id", ""), new BBAttribute("id", "id")),
            new BBTag("group", "<a href=\"/groups/${id}/id\">", "</a>", new BBAttribute("id", ""), new BBAttribute("id", "id")),
            new BBTag("room", "<a onclick=\"roomForward(this, '${id}', 'private'); return false;\" target=\"client\" href=\"/client?forwardId=2&amp;roomId=${id}\">", "</a>", new BBAttribute("id", ""), new BBAttribute("id", "id")),
            new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
            new BBTag("size", "<span style=\"font-size: ${size};\">", "</span>", new BBAttribute("size", "", ctx => ctx.AttributeValue == "small" ? "9px" : ctx.AttributeValue == "large" ? "14px" : "inherit")),
        });

        public static string Convert(string input)
        {
            if (input == null) return null;

            // An invalid value keeps the tag but drops the value, so the tags stay balanced.
            input = UrlTag.Replace(input, m => SafeUrl.IsMatch(m.Groups[1].Value) ? m.Value : "[url]");
            input = IdTag.Replace(input, m => Regex.IsMatch(m.Groups[2].Value, @"^\d{1,10}$") ? m.Value : "[" + m.Groups[1].Value + "]");

            return Parser.ToHtml(input).Replace("\n", "<br/>");
        }
    }
}
