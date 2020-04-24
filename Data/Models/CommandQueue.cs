using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("command_queue")]
    public class CommandQueue
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("executed")]
        public int Executed { get; set; }
        [Column("command")]
        public string Command { get; set; }
        [Column("arguments")]
        public string Arguments { get; set; }
        [Column("time")]
        public long Time { get; set; }
    }
}
