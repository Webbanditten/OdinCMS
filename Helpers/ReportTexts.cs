using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Helpers
{
    /// <summary>
    /// The texts of the Home's report mode (set "report", editable under Housekeeping > Localization): the toolbar buttons and
    /// the dialogs homeauth.js asks /mod/localizations for. The Danish here is used while a row is missing.
    /// </summary>
    public static class ReportTexts
    {
        public const string Set = "report";

        private static readonly Dictionary<string, string> Defaults = new()
        {
            ["button_start"] = "Rapporter",
            ["button_stop"] = "Stop rapportering",
            ["dialog_report"] = "Rapporter",
            ["dialog_cancel"] = "Annuller",
            ["dialog_ok"] = "OK",
            ["name_title"] = "Rapporter Habbo-navn",
            ["name_message"] = "Synes du, at dette Habbo-navn er stødende? Så kan du rapportere det til vores moderatorer.",
            ["motto_title"] = "Rapporter motto",
            ["motto_message"] = "Synes du, at dette motto er stødende eller chikanerende? Så kan du rapportere det til vores moderatorer.",
            ["stickie_title"] = "Rapporter note",
            ["stickie_message"] = "Synes du, at denne note er stødende eller chikanerende? Så kan du rapportere den til vores moderatorer.",
            ["guestbook_title"] = "Rapporter indlæg",
            ["guestbook_message"] = "Synes du, at dette indlæg i gæstebogen er stødende eller chikanerende? Så kan du rapportere det til vores moderatorer.",
            ["room_title"] = "Rapporter rum",
            ["room_message"] = "Synes du, at rummets navn eller beskrivelse er stødende? Så kan du rapportere det til vores moderatorer.",
            ["groupname_title"] = "Rapporter gruppenavn",
            ["groupname_message"] = "Synes du, at gruppens navn er stødende? Så kan du rapportere det til vores moderatorer.",
            ["groupdesc_title"] = "Rapporter gruppebeskrivelse",
            ["groupdesc_message"] = "Synes du, at gruppens beskrivelse er stødende eller chikanerende? Så kan du rapportere den til vores moderatorer.",
            ["success_title"] = "Tak for din rapport",
            ["success_message"] = "Vores moderatorer kigger på det så hurtigt som muligt.",
            ["spam_title"] = "Allerede rapporteret",
            ["spam_message"] = "Du har allerede rapporteret dette, eller du har sendt mange rapporter på kort tid. Prøv igen senere.",
            ["error_title"] = "Det gik galt",
            ["error_message"] = "Din rapport kunne ikke sendes. Prøv igen senere.",
        };

        /// <summary>Every key with its default, for the migration and Housekeeping.</summary>
        public static IReadOnlyDictionary<string, string> All => Defaults;

        public static string T(string key) => Texts.T(key, Set, Defaults.GetValueOrDefault(key, key));

        /// <summary>
        /// /mod/localizations: per dialog its title and message plus the button texts (HTML: homeauth.js puts them into its
        /// dialog template as they are). The report dialogs have Cancel + Report, the result dialogs (success, spam, error) one button.
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> Dialogs()
        {
            var dialogs = new Dictionary<string, Dictionary<string, string>>();
            foreach (var type in WebsiteReports.Types.Keys)
            {
                dialogs[type] = new Dictionary<string, string>
                {
                    ["title"] = T(type + "_title"),
                    ["message"] = T(type + "_message"),
                    ["btnCancelText"] = T("dialog_cancel"),
                    ["btnReportText"] = T("dialog_report"),
                };
            }
            foreach (var result in new[] { "success", "spam", "error" })
            {
                dialogs[result] = new Dictionary<string, string>
                {
                    ["title"] = T(result + "_title"),
                    ["message"] = T(result + "_message"),
                    ["btnText"] = T("dialog_ok"),
                };
            }
            return dialogs;
        }
    }
}
