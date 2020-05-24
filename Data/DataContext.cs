using Microsoft.EntityFrameworkCore;
using KeplerCMS.Data.Models;
namespace KeplerCMS.Data
{
    public class DataContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsersBadges>()
                .HasKey(c => new { c.UserId, c.Badge });

            modelBuilder.Entity<Friends>()
                .HasKey(c => new { c.FromId, c.ToId });

            modelBuilder.Entity<FriendRequests>()
                .HasKey(c => new { c.FromId, c.ToId });
        }
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

        public DbSet<Homes> Homes { get; set; }
        public DbSet<HomesCatalog> HomesCatalog { get; set; }
        public DbSet<HomesCategories> HomesCategories { get; set; }
        public DbSet<HomesItems> HomesItems { get; set; }
        public DbSet<HomesItemData> HomesItemData { get; set; }
        public DbSet<HomesInventory> HomesInventory { get; set; }
        public DbSet<HomesRating> HomesRating { get; set; }
        public DbSet<HomesGuestbook> HomesGuestbook { get; set; }
        public DbSet<Rooms> Rooms { get; set; }
        public DbSet<SoundMachineSongs> SoundMachineSongs { get; set; }
        public DbSet<UsersBadges> UsersBadges { get; set; }

        public DbSet<Friends> Friends { get; set; }
        public DbSet<FriendRequests> FriendRequests { get; set; }
        public DbSet<CatalogueItems> CatalogueItems { get; set; }
        public DbSet<CataloguePages> CataloguePages { get; set; }
        public DbSet<Items> Items { get; set; }
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }
    }
}
