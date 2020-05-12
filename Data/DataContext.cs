using Microsoft.EntityFrameworkCore;
using KeplerCMS.Data.Models;
namespace KeplerCMS.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Users> Users { get; set; }
        public DbSet<CommandQueue> CommandQueue { get; set; }
        public DbSet<Menu> Menu { get; set; }
        public DbSet<Containers> Containers { get; set; }
        public DbSet<Pages> Pages { get; set; }
        public DbSet<Fuses> Fuses { get; set; }
        public DbSet<Upload> Uploads { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<Promo> Promos { get; set; }
        public DbSet<Vouchers> Vouchers { get; set; }
        public DbSet<VoucherHistory> VoucherHistory { get; set; }
        public DbSet<Settings> Settings { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }
    }
}
