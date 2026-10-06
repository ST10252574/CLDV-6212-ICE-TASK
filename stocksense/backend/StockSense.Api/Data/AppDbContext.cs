using Microsoft.EntityFrameworkCore;
using StockSense.Api.Models;

namespace StockSense.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(254).IsRequired();
            e.Property(u => u.ShopName).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(120).IsRequired();
            e.Property(p => p.Sku).HasMaxLength(40).IsRequired();
            e.Property(p => p.Category).HasMaxLength(60);
            e.Property(p => p.CostPrice).HasPrecision(12, 2);
            e.Property(p => p.SellPrice).HasPrecision(12, 2);

            // A SKU only has to be unique inside one shop.
            e.HasIndex(p => new { p.OwnerId, p.Sku }).IsUnique();

            e.HasOne(p => p.Owner)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.Property(m => m.Reason).HasMaxLength(200).IsRequired();
            e.HasOne(m => m.Product)
                .WithMany(p => p.Movements)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
