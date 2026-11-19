using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using KeplerCMS.Data.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Westwind.Globalization;

namespace KeplerCMS.Helpers
{
    /// <summary>
    /// Every text of the Habbo Help Tool (/iot, Views/Iot, IotController): key, default Danish and where it shows. Saved
    /// texts are cms_localizations rows (set "iot", locale da-DK); without a row the default here shows. Staff with
    /// housekeeping_localization edit them on the page itself: /iot/go?edit=1 (cookie iot_edit) opens a panel with the
    /// page's texts and links to every step (Views/Iot/_TextEditor.cshtml, saved by IotController.SaveTexts).
    /// Texts are HTML (links allowed); a line break shows as one. An empty text hides its slot where the page has one.
    /// </summary>
    public static class IotTexts
    {
        public const string Set = "iot";
        public const string Locale = "da-DK";
        public const string EditCookie = "iot_edit";
        /// <summary>A text staff emptied (the localization library treats an empty value as missing and shows the default).</summary>
        public const string Empty = "<!-- empty -->";

        public record Text(string Key, string Default, string Where);

        private static readonly Text[] Fixed =
        {
            new("title", "Habbo Hjælpeværktøj", "Every page: the name in the title bar and the browser tab (plain text)"),
            new("faq", "Husk, at svarene på de mest almindelige spørgsmål fra Habbo'er og deres forældre allerede står på vores <a href=\"/help\">hjælpesider</a>. Står svaret der, svarer vi ikke på din henvendelse, så vi kan nå de andre.", "Steps 1-4: the bold reminder at the top"),

            new("intro_title", "", "Step 1: a heading above everything (empty = hidden)"),
            new("intro", "", "Step 1: a paragraph above everything (empty = hidden)"),
            new("step1_title", "Har du allerede en Habbo?", "Step 1: heading"),
            new("step1_yes", "Ja, jeg bruger Habbo", "Step 1: first answer"),
            new("step1_no", "Nej, jeg har ikke en Habbo", "Step 1: second answer"),
            new("button_next", "Næste", "Steps 1-3: the button (plain text)"),
            new("urgent_title", "Akut hjælp", "Step 1: heading of the shortcut box"),
            new("urgent_intro", "", "Step 1: text under the shortcut heading (empty = hidden)"),
            new("urgent_password", "Har du glemt din adgangskode?", "Step 1: first shortcut (links to the forgotten password page)"),
            new("urgent_password_text", "Få en ny med det samme.", "Step 1: under the first shortcut"),
            new("urgent_ban", "Er du smidt ud?", "Step 1: second shortcut (links to the ban page)"),
            new("urgent_ban_text", "Se, hvor længe du er udelukket.", "Step 1: under the second shortcut"),
            new("mine_title", "Dine henvendelser", "Step 1, logged in: heading over the player's own tickets"),

            new("step2_title_member", "Hvem er du?", "Step 2 (has a Habbo): heading"),
            new("step2_title_guest", "Hvordan kan vi svare dig?", "Step 2 (no Habbo): heading"),
            new("step2_name", "Dit Habbo-navn:", "Step 2: label of the name field"),
            new("step2_email", "Din e-mail:", "Step 2: label of the e-mail field"),
            new("step2_note", "Vi skriver til din e-mail, når vi har svaret på din henvendelse. Vi bruger den ikke til andet.", "Step 2: under the fields"),

            new("step3_title", "Hvad har du brug for hjælp med?", "Step 3: heading"),
            new("step3_intro", "Vælg den kategori, der passer bedst. Så kommer din henvendelse hurtigere til den, der kan hjælpe dig.", "Step 3: text over the list"),
            new("step3_choose", "Vælg ...", "Step 3: the list's first line (plain text)"),
        };

        private static readonly Text[] AfterIssues =
        {
            new("step4_title", "Beskriv dit problem", "Step 4: heading"),
            new("step4_intro", "Fortæl os så præcist som muligt, hvad der er sket, og hvornår. Skriv aldrig din adgangskode.", "Step 4: text over the text box"),
            new("step4_note", "", "Step 4: text under the text box (empty = hidden)"),
            new("button_send", "Send", "Step 4 and the ticket page: the button (plain text)"),

            new("step5_title", "Tak for din henvendelse!", "Thank-you page: heading"),
            new("step5_text", "Vi har modtaget din henvendelse og svarer, så snart vi kan.", "Thank-you page: first text"),
            new("step5_email", "Vi skriver til {email}, når der er svar.", "Thank-you page: after the first text when the player gave an e-mail ({email} = the address)"),
            new("step5_link", "Her kan du se dit svar og skrive til os igen. Gem linket, det er kun til dig:", "Thank-you page: over the ticket's link"),
            new("step5_note", "", "Thank-you page: text under the link (empty = hidden)"),
            new("back", "Tilbage til Habbo", "Thank-you and ban pages: the link back to the site"),

            new("ticket_title", "Din henvendelse", "Ticket page: heading"),
            new("ticket_sent", "Tak! Dit svar er sendt.", "Ticket page: after the player wrote again"),
            new("ticket_you", "Dig", "Ticket page: the player's name when they gave none (plain text)"),
            new("ticket_staff", "Habbo Staff", "Ticket page: who staff's answers are from (plain text)"),
            new("ticket_write", "Skriv til os:", "Ticket page: over the reply box"),
            new("ticket_closed", "Henvendelsen er lukket. Har du brug for mere hjælp, så <a href=\"/iot/go\">send en ny</a>.", "Ticket page: instead of the reply box when the ticket is closed"),
            new("status_0", "Ny", "Ticket status: new (step 1's list, ticket page, Housekeeping; plain text)"),
            new("status_1", "Besvaret", "Ticket status: answered (plain text)"),
            new("status_2", "Svar fra spilleren", "Ticket status: the player wrote again (plain text)"),
            new("status_3", "Lukket", "Ticket status: closed (plain text)"),

            new("ban_title", "Er du udelukket?", "Ban page: heading"),
            new("ban_login", "<a href=\"/account\">Log ind på hjemmesiden</a>, så kan du se her, om du er udelukket fra hotellet, og hvor længe.", "Ban page: when the visitor isn't logged in"),
            new("ban_none", "Du er ikke udelukket fra hotellet.", "Ban page: the player has no ban"),
            new("ban_until", "Du er udelukket fra hotellet indtil {until}.", "Ban page: the player's ban ({until} = date and time)"),
            new("ban_reason", "Begrundelse:", "Ban page: before the ban's reason"),
            new("ban_help", "Mener du, det er en fejl, så <a href=\"/iot/go\">skriv til os</a>.", "Ban page: under the answer"),

            new("err_choose", "Vælg et af svarene.", "Error, step 1: no answer picked"),
            new("err_name", "Skriv dit Habbo-navn.", "Error, step 2: no name"),
            new("err_email_needed", "Skriv en e-mail-adresse, så vi kan give dig besked, når vi har svaret.", "Error, step 2: no e-mail (not logged in)"),
            new("err_email_bad", "Den e-mail-adresse ser forkert ud.", "Error, step 2: not an e-mail address"),
            new("err_issue", "Vælg hvad din henvendelse handler om.", "Error, step 3: no category"),
            new("err_short", "Beskriv dit problem med lidt flere ord.", "Error, step 4: text too short"),
            new("err_flood", "Du har sendt mange henvendelser på kort tid. Vent lidt, og prøv igen.", "Error, step 4: too many tickets in an hour"),
            new("err_closed", "Henvendelsen er lukket. Send en ny, hvis du stadig har brug for hjælp.", "Error, ticket page: writing on a closed ticket"),
            new("err_reply_empty", "Skriv dit svar først.", "Error, ticket page: empty reply"),
        };

        /// <summary>In page order (the editor lists a page's texts in this order).</summary>
        public static readonly IReadOnlyList<Text> All = Fixed
            .Concat(HelpTickets.Issues.Select(i => new Text("issue_" + i.Key, i.Label, "Step 3: a category (also on the ticket pages and in Housekeeping; plain text)")))
            .Concat(AfterIssues)
            .ToArray();

        private static readonly Dictionary<string, (Text Text, int Index)> ByKey =
            All.Select((t, i) => (t, i)).ToDictionary(x => x.t.Key, x => x);

        public static Text Find(string key) => ByKey.TryGetValue(key, out var t) ? t.Text : null;

        public static int IndexOf(string key) => ByKey.TryGetValue(key, out var t) ? t.Index : int.MaxValue;

        /// <summary>The saved text, else the default. (DbRes adds a missing key as an invariant row whose value is the key.)</summary>
        public static string Get(string key)
        {
            var text = Find(key) ?? throw new ArgumentException("Unknown help tool text: " + key);
            string value;
            try { value = DbRes.T(key, Set, Locale); }
            catch (Exception) { value = null; }
            if (value == Empty) return "";
            return value == null || value == key ? text.Default : value;
        }

        /// <summary>Get as plain text: tags removed, entities decoded.</summary>
        public static string Plain(string key) => WebUtility.HtmlDecode(Regex.Replace(Get(key), "<[^>]*>", "")).Trim();

        public static bool CanEdit(Users user) =>
            user?.Fuses?.Any(f => f.FuseName == nameof(KeplerCMS.Models.Fuse.housekeeping_localization)) == true;

        /// <summary>
        /// Saves the texts staff changed: one equal to its default deletes the saved row, anything else is stored as typed.
        /// Returns how many changed.
        /// </summary>
        public static int Save(IDictionary<string, string> values)
        {
            var manager = DbResourceDataManager.CreateDbResourceDataManager();
            var changed = 0;
            foreach (var (key, raw) in values)
            {
                var text = Find(key);
                if (text == null) continue;
                var value = (raw ?? "").Replace("\r\n", "\n").Trim();
                if (value == Get(key)) continue;
                if (value == text.Default) manager.DeleteResource(key, Set, Locale);
                else manager.UpdateOrAddResource(key, value.Length == 0 ? Empty : value, Locale, Set);
                changed++;
            }
            if (changed > 0) DbResourceConfiguration.ClearResourceCache();
            return changed;
        }
    }

    /// <summary>The help tool views' texts (IotTexts); in edit mode they are marked so a click finds them in the editor.</summary>
    public static class IotTextHtml
    {
        private const string UsedItem = "iotTextsUsed";
        private const string EditingItem = "iotTextsEditing";

        /// <summary>The keys this page showed so far (the editor's "on this page").</summary>
        public static List<string> Used(HttpContext http)
        {
            if (http.Items[UsedItem] is not List<string> used) http.Items[UsedItem] = used = new List<string>();
            return used;
        }

        public static bool IotEditing(this IHtmlHelper html)
        {
            var http = html.ViewContext.HttpContext;
            if (http.Items[EditingItem] is bool editing) return editing;
            editing = IotTexts.CanEdit(html.ViewData["user"] as Users) && http.Request.Cookies[IotTexts.EditCookie] == "1";
            http.Items[EditingItem] = editing;
            return editing;
        }

        private static void Track(IHtmlHelper html, string key)
        {
            var used = Used(html.ViewContext.HttpContext);
            if (!used.Contains(key)) used.Add(key);
        }

        /// <summary>A text as HTML. Placeholders such as {email} get the (encoded) values given.</summary>
        public static IHtmlContent IotText(this IHtmlHelper html, string key, params (string Name, string Value)[] values)
        {
            Track(html, key);
            var value = IotTexts.Get(key);
            foreach (var (name, v) in values) value = value.Replace("{" + name + "}", WebUtility.HtmlEncode(v ?? ""));
            var body = value.Replace("\r\n", "\n").Replace("\n", "<br />");
            if (!html.IotEditing()) return new HtmlString(body);
            var empty = value.Trim().Length == 0;
            return new HtmlString($"<span class=\"iot-t{(empty ? " iot-empty" : "")}\" data-k=\"{key}\" title=\"{key}\">{(empty ? "[" + key + "]" : body)}</span>");
        }

        /// <summary>A text as plain text, for buttons, list lines and the title. A key that isn't a text (an old category) shows the fallback.</summary>
        public static string IotPlain(this IHtmlHelper html, string key, string fallback = null)
        {
            if (IotTexts.Find(key) == null) return fallback ?? key;
            Track(html, key);
            return IotTexts.Plain(key);
        }

        /// <summary>Whether an optional slot shows: it has text, or staff are editing (they see an empty marker).</summary>
        public static bool IotHas(this IHtmlHelper html, string key) => html.IotEditing() || IotTexts.Get(key).Trim().Length > 0;
    }
}
