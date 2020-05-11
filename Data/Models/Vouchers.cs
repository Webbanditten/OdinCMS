using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("vouchers")]
    public class Vouchers
    {
        [Key]
        [Column("voucher_code")]
        public string VoucherCode { get; set; }
        [Column("credits")]
        public int Credits { get; set; }
        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }
        [Column("is_single_use")]
        public int IIsSingleUse { get; set; }

        [NotMapped]
        public bool IsSingleUse
        {
            get { return IIsSingleUse == 1; }
            set { IIsSingleUse = value ? 1 : 0; }
        }
    }
}
