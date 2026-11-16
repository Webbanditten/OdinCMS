using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// One row per player per finished Battle Ball / SnowStorm game, written by Odin (GameFinishTask).
    /// game_id, team_id, team_score and won are NULL for games from before Odin v1.25.7.
    /// </summary>
    [Table("highscores")]
    public class Highscores
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("uid")]
        public int UserId { get; set; }

        /// <summary>"battleball" or "snowstorm".</summary>
        [Column("gametype")]
        public string GameType { get; set; }

        [Column("score")]
        public int Score { get; set; }

        [Column("game_id")]
        public int? GameId { get; set; }

        [Column("team_id")]
        public int? TeamId { get; set; }

        [Column("team_score")]
        public int? TeamScore { get; set; }

        [Column("won")]
        public bool? Won { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }
    }
}
