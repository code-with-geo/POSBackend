using Microsoft.EntityFrameworkCore;
using POS.Models;

namespace POS.Datas
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Products> Products { get; set; }

        // Overriding OnModelCreating to configure decimal precision
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the decimal precision for the Price property of Product
            modelBuilder.Entity<Products>(entity =>
            {
                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(p => p.Price).HasPrecision(18, 2);
            });
        }
    }
}
