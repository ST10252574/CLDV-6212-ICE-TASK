using Microsoft.EntityFrameworkCore;
using StockSense.Api.Data;
using StockSense.Api.Dtos;
using StockSense.Api.Models;

namespace StockSense.Api.Services;

/// <summary>Outcome of a service call. StatusCode lets controllers map failures to HTTP responses.</summary>
public record ServiceResult<T>(T? Value, string? Error = null, int StatusCode = 200)
{
    public bool Success => Error is null;
}

public interface IStockService
{
    Task<List<Product>> ListProductsAsync(int ownerId, string? search);
    Task<Product?> GetProductAsync(int ownerId, int productId);
    Task<ServiceResult<Product>> CreateProductAsync(int ownerId, ProductRequest request);
    Task<ServiceResult<Product>> UpdateProductAsync(int ownerId, int productId, ProductRequest request);
    Task<bool> DeleteProductAsync(int ownerId, int productId);
    Task<ServiceResult<Product>> AdjustStockAsync(int ownerId, int productId, StockAdjustmentRequest request);
    Task<List<StockMovement>?> GetMovementsAsync(int ownerId, int productId);
    Task<List<LowStockItem>> GetLowStockAsync(int ownerId);
    Task<DashboardSummary> GetSummaryAsync(int ownerId);
}

public class StockService : IStockService
{
    private readonly AppDbContext _db;

    public StockService(AppDbContext db)
    {
        _db = db;
    }

    // Every query is filtered by ownerId so one shop can never see or touch another shop's data.

    public async Task<List<Product>> ListProductsAsync(int ownerId, string? search)
    {
        var query = _db.Products.Where(p => p.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public Task<Product?> GetProductAsync(int ownerId, int productId) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.OwnerId == ownerId);

    public async Task<ServiceResult<Product>> CreateProductAsync(int ownerId, ProductRequest request)
    {
        var sku = NormalizeSku(request.Sku);

        if (await _db.Products.AnyAsync(p => p.OwnerId == ownerId && p.Sku == sku))
        {
            return new ServiceResult<Product>(null, $"SKU '{sku}' already exists in your shop.", 409);
        }

        var product = new Product
        {
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Sku = sku,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            CostPrice = request.CostPrice,
            SellPrice = request.SellPrice,
            Quantity = request.Quantity,
            ReorderLevel = request.ReorderLevel
        };

        if (request.Quantity > 0)
        {
            product.Movements.Add(new StockMovement { Change = request.Quantity, Reason = "Opening stock" });
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return new ServiceResult<Product>(product);
    }

    public async Task<ServiceResult<Product>> UpdateProductAsync(int ownerId, int productId, ProductRequest request)
    {
        var product = await GetProductAsync(ownerId, productId);
        if (product is null)
        {
            return new ServiceResult<Product>(null, "Product not found.", 404);
        }

        var sku = NormalizeSku(request.Sku);
        if (await _db.Products.AnyAsync(p => p.OwnerId == ownerId && p.Sku == sku && p.Id != productId))
        {
            return new ServiceResult<Product>(null, $"SKU '{sku}' already exists in your shop.", 409);
        }

        // If the quantity was edited directly, keep the audit trail honest.
        var difference = request.Quantity - product.Quantity;
        if (difference != 0)
        {
            product.Movements.Add(new StockMovement { Change = difference, Reason = "Manual correction" });
        }

        product.Name = request.Name.Trim();
        product.Sku = sku;
        product.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        product.CostPrice = request.CostPrice;
        product.SellPrice = request.SellPrice;
        product.Quantity = request.Quantity;
        product.ReorderLevel = request.ReorderLevel;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return new ServiceResult<Product>(product);
    }

    public async Task<bool> DeleteProductAsync(int ownerId, int productId)
    {
        var product = await GetProductAsync(ownerId, productId);
        if (product is null)
        {
            return false;
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ServiceResult<Product>> AdjustStockAsync(int ownerId, int productId, StockAdjustmentRequest request)
    {
        var product = await GetProductAsync(ownerId, productId);
        if (product is null)
        {
            return new ServiceResult<Product>(null, "Product not found.", 404);
        }

        if (request.Change == 0)
        {
            return new ServiceResult<Product>(null, "Change must not be zero.", 400);
        }

        if (product.Quantity + request.Change < 0)
        {
            return new ServiceResult<Product>(null,
                $"Not enough stock. Only {product.Quantity} unit(s) available.", 400);
        }

        product.Quantity += request.Change;
        product.UpdatedAt = DateTime.UtcNow;
        product.Movements.Add(new StockMovement { Change = request.Change, Reason = request.Reason.Trim() });

        await _db.SaveChangesAsync();
        return new ServiceResult<Product>(product);
    }

    public async Task<List<StockMovement>?> GetMovementsAsync(int ownerId, int productId)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == productId && p.OwnerId == ownerId);
        if (!exists)
        {
            return null;
        }

        return await _db.StockMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<LowStockItem>> GetLowStockAsync(int ownerId)
    {
        var products = await _db.Products
            .Where(p => p.OwnerId == ownerId && p.Quantity <= p.ReorderLevel)
            .OrderBy(p => p.Quantity)
            .ToListAsync();

        return products.Select(p =>
        {
            var item = new LowStockItem
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Category = p.Category,
                CostPrice = p.CostPrice,
                SellPrice = p.SellPrice,
                Quantity = p.Quantity,
                ReorderLevel = p.ReorderLevel,
                IsLowStock = true,
                UpdatedAt = p.UpdatedAt,
                SuggestedReorderQuantity = Math.Max(p.ReorderLevel * 2 - p.Quantity, 1)
            };
            return item;
        }).ToList();
    }

    public async Task<DashboardSummary> GetSummaryAsync(int ownerId)
    {
        // A small shop has hundreds of products, not millions, so aggregating in memory is fine and
        // keeps the logic identical on Postgres and on the in-memory provider used by the unit tests.
        var products = await _db.Products.Where(p => p.OwnerId == ownerId).ToListAsync();

        return new DashboardSummary
        {
            TotalProducts = products.Count,
            TotalUnits = products.Sum(p => p.Quantity),
            InventoryCostValue = products.Sum(p => p.CostPrice * p.Quantity),
            InventoryRetailValue = products.Sum(p => p.SellPrice * p.Quantity),
            LowStockCount = products.Count(p => p.Quantity <= p.ReorderLevel),
            OutOfStockCount = products.Count(p => p.Quantity == 0)
        };
    }

    private static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();
}
