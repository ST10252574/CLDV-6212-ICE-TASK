using System.ComponentModel.DataAnnotations;
using StockSense.Api.Models;

namespace StockSense.Api.Dtos;

// ---------- Auth ----------

public class RegisterRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string ShopName { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
}

// ---------- Products ----------

public class ProductRequest
{
    [Required, StringLength(120, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Category { get; set; }

    [Range(0, 1_000_000)]
    public decimal CostPrice { get; set; }

    [Range(0, 1_000_000)]
    public decimal SellPrice { get; set; }

    [Range(0, 1_000_000)]
    public int Quantity { get; set; }

    [Range(0, 1_000_000)]
    public int ReorderLevel { get; set; }
}

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsLowStock { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ProductResponse From(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Sku = p.Sku,
        Category = p.Category,
        CostPrice = p.CostPrice,
        SellPrice = p.SellPrice,
        Quantity = p.Quantity,
        ReorderLevel = p.ReorderLevel,
        IsLowStock = p.Quantity <= p.ReorderLevel,
        UpdatedAt = p.UpdatedAt
    };
}

public class LowStockItem : ProductResponse
{
    /// <summary>Simple heuristic: restock up to twice the reorder level.</summary>
    public int SuggestedReorderQuantity { get; set; }
}

// ---------- Stock movements ----------

public class StockAdjustmentRequest
{
    /// <summary>Positive to receive stock, negative to record a sale or loss.</summary>
    [Range(-1_000_000, 1_000_000)]
    public int Change { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;
}

public class StockMovementResponse
{
    public int Id { get; set; }
    public int Change { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ---------- Dashboard ----------

public class DashboardSummary
{
    public int TotalProducts { get; set; }
    public int TotalUnits { get; set; }
    public decimal InventoryCostValue { get; set; }
    public decimal InventoryRetailValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}
