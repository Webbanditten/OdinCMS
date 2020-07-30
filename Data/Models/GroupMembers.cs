using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("group_members")]
    public class GroupMembers
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("group_")]
        public int GroupId { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("rights")]
        public int IRights { get; set; }

        [Column("pending")]
        public int IPending { get; set; }

        [NotMapped]
        public bool Rights
        {
            get { return IRights == 1; }
            set { IRights = value ? 1 : 0; }
        }

        [NotMapped]
        public bool Pending
        {
            get { return IPending == 1; }
            set { IPending = value ? 1 : 0; }
        }
    }
}
