namespace StockSense.Api.Models;

/// <summary>A shop owner who signs in to StockSense. Every product belongs to exactly one owner.</summary>
public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Product> Products { get; set; } = new();
}

/// <summary>A stock item the shop sells.</summary>
public class Product
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public AppUser? Owner { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Category { get; set; }

    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary>When Quantity falls to or below this level the product is flagged for reorder.</summary>
    public int ReorderLevel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<StockMovement> Movements { get; set; } = new();
}

/// <summary>An audit-trail entry: stock received (+) or sold / lost (-).</summary>
public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Positive = stock in, negative = stock out.</summary>
    public int Change { get; set; }

    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
