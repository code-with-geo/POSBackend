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
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Locations> Locations { get; set; }


        // Overriding OnModelCreating to configure decimal precision
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the decimal precision for the Price property of Product
            modelBuilder.Entity<Products>(entity =>
            {
                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(p => p.Price).HasPrecision(18, 2);
                entity.Property(c => c.DateCreated).HasDefaultValueSql("GETDATE()");

                // If there are relationships, configure them here
                // Example: One-to-Many relationship with Products
                entity.HasMany(c => c.Inventory)
                      .WithOne(p => p.Products)
                      .HasForeignKey(p => p.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Categories>(entity =>
            {
                // Define the primary key
                entity.HasKey(c => c.CategoryId);

                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(c => c.DateCreated).HasDefaultValueSql("GETDATE()");

                // Additional configurations can be added here as needed
                // Example: Max length for Name
                entity.Property(c => c.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                // If there are relationships, configure them here
                // Example: One-to-Many relationship with Products
                entity.HasMany(c => c.Products)
                      .WithOne(p => p.Categories)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Inventory>(entity =>
            {
                // Define the primary key
                entity.HasKey(i => i.InventoryId);

                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(i => i.DateCreated).HasDefaultValueSql("GETDATE()");

            });


            modelBuilder.Entity<Locations>(entity =>
            {
                // Define the primary key
                entity.HasKey(l => l.LocationId);

                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(l => l.DateCreated).HasDefaultValueSql("GETDATE()");

                entity.HasMany(c => c.Inventory)
                      .WithOne(p => p.Locations)
                      .HasForeignKey(p => p.LocationId)
                      .OnDelete(DeleteBehavior.Cascade);

            });
        }
    }
}
