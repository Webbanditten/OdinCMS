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

        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }
    }
}
