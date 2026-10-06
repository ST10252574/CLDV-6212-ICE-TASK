using Microsoft.EntityFrameworkCore;
using StockSense.Api.Models;

namespace StockSense.Api.Data;


public static class DemoDataSeeder
{
    public const string DemoEmail = "demo@stocksense.app";
    public const string DemoPassword = "Demo1234!";

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Email == DemoEmail))
        {
            return;
        }

        var user = new AppUser
        {
            Email = DemoEmail,
            ShopName = "Thandi's Corner Shop",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword)
        };

        user.Products.AddRange(new[]
        {
            Item("Maize Meal 5kg", "MAIZE-5KG", "Groceries", 48m, 62m, 24, 10),
            Item("Sunflower Oil 750ml", "OIL-750", "Groceries", 36m, 48m, 4, 8),
            Item("White Bread", "BREAD-WHT", "Bakery", 14m, 19m, 0, 12),
            Item("Full Cream Milk 1L", "MILK-1L", "Dairy", 17m, 24m, 18, 12),
            Item("Airtime R10", "AIR-R10", "Airtime", 9.2m, 10m, 60, 20),
            Item("Sugar 2.5kg", "SUGAR-25", "Groceries", 42m, 55m, 7, 6),
            Item("Cooldrink 2L", "COLA-2L", "Beverages", 20m, 29m, 3, 10),
            Item("Washing Powder 1kg", "WASH-1KG", "Household", 38m, 52m, 15, 5)
        });

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private static Product Item(string name, string sku, string category, decimal cost, decimal sell, int qty, int reorder)
    {
        var product = new Product
        {
            Name = name,
            Sku = sku,
            Category = category,
            CostPrice = cost,
            SellPrice = sell,
            Quantity = qty,
            ReorderLevel = reorder
        };

        if (qty > 0)
        {
            product.Movements.Add(new StockMovement { Change = qty, Reason = "Opening stock" });
        }

        return product;
    }
}
