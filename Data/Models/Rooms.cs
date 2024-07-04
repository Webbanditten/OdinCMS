﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using MySql.EntityFrameworkCore.DataAnnotations;

namespace KeplerCMS.Data.Models
{
    [Table("rooms")]
    public class Rooms
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("owner_id")]
        [ForeignKey("Users")]
        public int OwnerId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("accesstype")]
        public int AccessType { get; set; }
        [Column("showname")]
        public int ShowOwner { get; set; }
        [Column("category")]
        public int Category { get; set; }
        [Column("model")]
        [MySqlCollation("utf8mb4_bin")]
        public string Model { get; set; }
        [Column("ccts")]
        public string Casts { get; set; }
        
        public Users Owner { get; set; }
    }
}
