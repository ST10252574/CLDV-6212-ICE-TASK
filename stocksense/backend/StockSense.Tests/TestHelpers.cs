using Microsoft.EntityFrameworkCore;
using StockSense.Api.Data;
using StockSense.Api.Dtos;

namespace StockSense.Tests;

internal static class TestHelpers
{
    /// <summary>A fresh, isolated in-memory database for every test.</summary>
    public static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static ProductRequest Product(
        string name = "Maize Meal 5kg",
        string sku = "MAIZE-5KG",
        int quantity = 10,
        int reorderLevel = 5,
        decimal cost = 50m,
        decimal sell = 65m) => new()
    {
        Name = name,
        Sku = sku,
        Category = "Groceries",
        CostPrice = cost,
        SellPrice = sell,
        Quantity = quantity,
        ReorderLevel = reorderLevel
    };
}
