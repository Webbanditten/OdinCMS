using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// A player's report of something on a Habbo Home or group page (ReportController, the Home's "Rapporter" mode), handled
    /// in Housekeeping > Customer service > Website reports. <see cref="Content"/> is the text as it was when reported, so it
    /// stays readable after the player changes or deletes it.
    /// </summary>
    [Table("cms_reports")]
    public class WebsiteReport
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        /// <summary>One of <see cref="WebsiteReports.Types"/>.</summary>
        [Column("type")]
        public string Type { get; set; }
        /// <summary>The user (name, motto), Home item (stickie), guestbook entry, room or group Home (groupname, groupdesc).</summary>
        [Column("object_id")]
        public int ObjectId { get; set; }
        /// <summary>The Home or group page it was on (cms_homes.id), for the link; null for a user without a Home.</summary>
        [Column("home_id")]
        public int? HomeId { get; set; }
        /// <summary>Who wrote it: the user, the note's or entry's writer, the room's or group's owner.</summary>
        [Column("author_id")]
        public int? AuthorId { get; set; }
        [Column("content")]
        public string Content { get; set; }
        [Column("reporter_id")]
        public int ReporterId { get; set; }
        [Column("ip")]
        public string Ip { get; set; }
        /// <summary><see cref="WebsiteReports.Open"/> or <see cref="WebsiteReports.Closed"/>.</summary>
        [Column("status")]
        public int Status { get; set; }
        /// <summary>What staff did, e.g. "Note deleted" or "No action".</summary>
        [Column("outcome")]
        public string Outcome { get; set; }
        [Column("closed_by")]
        public int? ClosedBy { get; set; }
        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public static class WebsiteReports
    {
        public const int Open = 0;
        public const int Closed = 1;

        public const string Name = "name";
        public const string Motto = "motto";
        public const string Stickie = "stickie";
        public const string Guestbook = "guestbook";
        public const string Room = "room";
        public const string GroupName = "groupname";
        public const string GroupDesc = "groupdesc";

        /// <summary>The kinds the Home's report buttons send (homeview.js / homeauth.js: /mod/add_TYPE_report), for staff.</summary>
        public static readonly IReadOnlyDictionary<string, string> Types = new Dictionary<string, string>
        {
            [Name] = "Habbo name",
            [Motto] = "Motto",
            [Stickie] = "Home note",
            [Guestbook] = "Guestbook entry",
            [Room] = "Room (name and description)",
            [GroupName] = "Group name",
            [GroupDesc] = "Group description",
        };

        public static string TypeLabel(string type) => type != null && Types.TryGetValue(type, out var label) ? label : type;

        /// <summary>The button that removes the reported text, or null when staff fix it elsewhere (a Habbo name, a room).</summary>
        public static string RemoveLabel(string type) => type switch
        {
            Stickie => "Delete the note",
            Guestbook => "Delete the entry",
            Motto => "Clear the motto",
            GroupDesc => "Clear the description",
            GroupName => "Rename the group",
            _ => null,
        };
    }
}
