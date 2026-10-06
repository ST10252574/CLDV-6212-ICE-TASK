using StockSense.Api.Dtos;
using StockSense.Api.Services;

namespace StockSense.Tests;

public class StockServiceTests
{
    private const int Owner = 1;
    private const int OtherOwner = 2;

    [Fact]
    public async Task CreateProduct_NormalizesSku_AndRecordsOpeningStock()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);

        var result = await service.CreateProductAsync(Owner, TestHelpers.Product(sku: "  maize-5kg ", quantity: 12));

        Assert.True(result.Success);
        Assert.Equal("MAIZE-5KG", result.Value!.Sku);
        var movements = await service.GetMovementsAsync(Owner, result.Value.Id);
        Assert.Single(movements!);
        Assert.Equal(12, movements![0].Change);
    }

    [Fact]
    public async Task CreateProduct_RejectsDuplicateSku_InSameShop()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        await service.CreateProductAsync(Owner, TestHelpers.Product());

        var duplicate = await service.CreateProductAsync(Owner, TestHelpers.Product(name: "Other name"));

        Assert.False(duplicate.Success);
        Assert.Equal(409, duplicate.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AllowsSameSku_InDifferentShops()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        await service.CreateProductAsync(Owner, TestHelpers.Product());

        var other = await service.CreateProductAsync(OtherOwner, TestHelpers.Product());

        Assert.True(other.Success);
    }

    [Fact]
    public async Task AdjustStock_ReceivingStock_IncreasesQuantity()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        var product = (await service.CreateProductAsync(Owner, TestHelpers.Product(quantity: 10))).Value!;

        var result = await service.AdjustStockAsync(Owner, product.Id,
            new StockAdjustmentRequest { Change = 15, Reason = "Delivery" });

        Assert.True(result.Success);
        Assert.Equal(25, result.Value!.Quantity);
    }

    [Fact]
    public async Task AdjustStock_CannotGoBelowZero()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        var product = (await service.CreateProductAsync(Owner, TestHelpers.Product(quantity: 3))).Value!;

        var result = await service.AdjustStockAsync(Owner, product.Id,
            new StockAdjustmentRequest { Change = -4, Reason = "Sale" });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(3, (await service.GetProductAsync(Owner, product.Id))!.Quantity);
    }

    [Fact]
    public async Task AdjustStock_ZeroChange_IsRejected()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        var product = (await service.CreateProductAsync(Owner, TestHelpers.Product())).Value!;

        var result = await service.AdjustStockAsync(Owner, product.Id,
            new StockAdjustmentRequest { Change = 0, Reason = "Nothing" });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task OneShop_CannotSeeOrChange_AnotherShopsProducts()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        var product = (await service.CreateProductAsync(Owner, TestHelpers.Product())).Value!;

        Assert.Null(await service.GetProductAsync(OtherOwner, product.Id));
        Assert.Empty(await service.ListProductsAsync(OtherOwner, null));
        Assert.False(await service.DeleteProductAsync(OtherOwner, product.Id));

        var adjust = await service.AdjustStockAsync(OtherOwner, product.Id,
            new StockAdjustmentRequest { Change = 1, Reason = "Hack" });
        Assert.Equal(404, adjust.StatusCode);
    }

    [Fact]
    public async Task LowStock_ListsOnlyItemsAtOrBelowReorderLevel_WithSuggestedQuantity()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        await service.CreateProductAsync(Owner, TestHelpers.Product(name: "Plenty", sku: "A", quantity: 50, reorderLevel: 10));
        await service.CreateProductAsync(Owner, TestHelpers.Product(name: "Edge", sku: "B", quantity: 10, reorderLevel: 10));
        await service.CreateProductAsync(Owner, TestHelpers.Product(name: "Empty", sku: "C", quantity: 0, reorderLevel: 8));

        var low = await service.GetLowStockAsync(Owner);

        Assert.Equal(2, low.Count);
        Assert.Equal("Empty", low[0].Name);              // lowest quantity first
        Assert.Equal(16, low[0].SuggestedReorderQuantity); // 8*2 - 0
        Assert.Equal(10, low[1].SuggestedReorderQuantity); // 10*2 - 10
    }

    [Fact]
    public async Task Summary_CalculatesTotalsAndValues()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        await service.CreateProductAsync(Owner, TestHelpers.Product(sku: "A", quantity: 10, reorderLevel: 2, cost: 5m, sell: 8m));
        await service.CreateProductAsync(Owner, TestHelpers.Product(sku: "B", quantity: 0, reorderLevel: 2, cost: 100m, sell: 150m));
        await service.CreateProductAsync(OtherOwner, TestHelpers.Product(sku: "C", quantity: 999));

        var summary = await service.GetSummaryAsync(Owner);

        Assert.Equal(2, summary.TotalProducts);
        Assert.Equal(10, summary.TotalUnits);
        Assert.Equal(50m, summary.InventoryCostValue);
        Assert.Equal(80m, summary.InventoryRetailValue);
        Assert.Equal(1, summary.LowStockCount);
        Assert.Equal(1, summary.OutOfStockCount);
    }

    [Fact]
    public async Task UpdateProduct_ChangingQuantity_WritesManualCorrectionMovement()
    {
        using var db = TestHelpers.NewDb();
        var service = new StockService(db);
        var product = (await service.CreateProductAsync(Owner, TestHelpers.Product(quantity: 10))).Value!;

        var update = TestHelpers.Product(quantity: 7);
        var result = await service.UpdateProductAsync(Owner, product.Id, update);

        Assert.True(result.Success);
        var movements = await service.GetMovementsAsync(Owner, product.Id);
        Assert.Contains(movements!, m => m.Change == -3 && m.Reason == "Manual correction");
    }
}
