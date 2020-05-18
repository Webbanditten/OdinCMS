using KeplerCMS.Data.Models;
using Narochno.BBCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo.Helpers
{
    public class BBCodeToString
    {

        public static string Convert(string input)
        {
            input = Regex.Replace(input, "<.*?>", String.Empty);
            input = input.Replace("[size=small]", "<span style=\"font-size: 9px;\">").Replace("[/size]", "</span>");
            input = input.Replace("[size=large]", "<span style=\"font-size: 14px;\">").Replace("[/size]", "</span>");

            var parser = new BBCodeParser(new[]
                {
                    new BBTag("b", "<b>", "</b>"),
                    new BBTag("i", "<i>", "</i>"),
                    new BBTag("u", "<u>", "</u>"),
                    new BBTag("code", "<pre>", "</pre>"),
                    new BBTag("quote", "<div class=\"bbcode-quote\">", "</div>"),
                    new BBTag("color", "<font color=\"${color}\">", "</font>", new BBAttribute("color", ""), new BBAttribute("color", "color")),
                    new BBTag("habbo", "<a href=\"/home/id/${id}\">", "</a>", new BBAttribute("id", ""), new BBAttribute("id", "id")),
                    new BBTag("room", "<a onclick=\"roomForward(this, '${id}', 'private'); return false;\" target=\"client\" href=\"/client?forwardId=2&amp;roomId=${id}\">", "</a>", new BBAttribute("id", ""), new BBAttribute("id", "id")),
                    new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
                });
            var output = parser.ToHtml(input);
            output = output.Replace("\n", "<br/>");
            return output;
        }
    }
}
