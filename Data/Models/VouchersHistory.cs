using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("vouchers_history")]
    public class VoucherHistory
    {
        [Key]
        [Column("voucher_code")]
        public string VoucherCode { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("used_at")]
        public DateTime UsedAt { get; set; }
        [Column("credits_redeemed")]
        public int? CreditsRedeemed { get; set; }
        [Column("items_redeemed")]
        public string ItemsRedeemed { get; set; }
    }
}
