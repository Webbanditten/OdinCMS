using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Models.Users> Users { get; set; }
        public DbSet<Models.CommandQueue> CommandQueue { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }
    }
}
