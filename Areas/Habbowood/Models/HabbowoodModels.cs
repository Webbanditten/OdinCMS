using System.Collections.Generic;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Westwind.Globalization;

namespace KeplerCMS.Areas.Habbowood
{
    /// <summary>The Habbowood pages' texts: cms_localizations (ResourceSet habbowood), with the Danish text as fallback.</summary>
    public static class HabbowoodText
    {
        public static string T(string key, string fallback)
        {
            var value = DbRes.T(key, "habbowood");
            return string.IsNullOrWhiteSpace(value) || value == key ? fallback : value;
        }
    }

    /// <summary>The studio (/habbowood): the 2007 movie editor in the site page.</summary>
    public class StudioViewModel
    {
        public Movies Movie { get; set; }
        public string Username { get; set; }
        public bool HabboClub { get; set; }
    }

    /// <summary>After saving in the editor (/habbowood/end): publish it, watch it or keep editing.</summary>
    public class EndViewModel
    {
        public Movies Movie { get; set; }
        public string Subtitle { get; set; }
    }

    /// <summary>A movie in the site page (/habbowood/watch/{id}) with its rating box.</summary>
    public class WatchViewModel
    {
        public Movies Movie { get; set; }
        public string Subtitle { get; set; }
        public string Director { get; set; }
        public double Rating { get; set; }
        public int Votes { get; set; }
        public bool LoggedIn { get; set; }
        public bool CanVote { get; set; }
        public bool IsOwner { get; set; }
        public List<HabbowoodListMovie> Top { get; set; }
    }

    /// <summary>A Habbowood Flash movie for Views/Shared/Habbowood/_Flash.cshtml.</summary>
    public class FlashEmbed
    {
        public string Id { get; set; }
        public string Swf { get; set; }
        public string Base { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Background { get; set; } = "#292929";
        public List<KeyValuePair<string, string>> Variables { get; set; } = new();
    }
}
