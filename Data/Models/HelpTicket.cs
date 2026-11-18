using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// A question sent through the Habbo Help Tool (/iot/go, IotController). The player follows it at
    /// /iot/ticket?id=ID&amp;token=TOKEN; staff answer it in Housekeeping > Help tickets. Status: HelpTickets.*.
    /// </summary>
    [Table("cms_help_tickets")]
    public class HelpTicket
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int? UserId { get; set; }
        /// <summary>The Habbo name the player gave (players without a Habbo leave it empty).</summary>
        [Column("name")]
        public string Name { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("issue")]
        public string Issue { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("token")]
        public string Token { get; set; }
        [Column("status")]
        public int Status { get; set; }
        [Column("ip")]
        public string Ip { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>A reply on a help ticket: from staff (StaffId) or from the player (StaffId null).</summary>
    [Table("cms_help_ticket_responses")]
    public class HelpTicketResponse
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("ticket_id")]
        public int TicketId { get; set; }
        [Column("staff_id")]
        public int? StaffId { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public static class HelpTickets
    {
        public const int New = 0;
        public const int Answered = 1;
        public const int PlayerReplied = 2;
        public const int Closed = 3;

        /// <summary>The help tool's problem categories (key, Danish label).</summary>
        public static readonly IReadOnlyList<(string Key, string Label)> Issues = new[]
        {
            ("login", "Jeg kan ikke logge ind"),
            ("credits", "Mønter og betaling"),
            ("club", "Habbo Club"),
            ("trade", "Møbler, handel og bytte"),
            ("scam", "Snyd og svindel"),
            ("ban", "Jeg er blevet udelukket"),
            ("bug", "Fejl i hotellet eller på hjemmesiden"),
            ("home", "Habbo Home og grupper"),
            ("other", "Andet"),
        };

        public static string IssueLabel(string key)
        {
            foreach (var (k, label) in Issues) if (k == key) return label;
            return key;
        }

        public static string StatusLabel(int status) => status switch
        {
            New => "Ny",
            Answered => "Besvaret",
            PlayerReplied => "Svar fra spilleren",
            Closed => "Lukket",
            _ => status.ToString(),
        };
    }
}
