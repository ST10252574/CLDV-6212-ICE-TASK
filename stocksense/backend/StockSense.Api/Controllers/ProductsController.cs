using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Api.Dtos;
using StockSense.Api.Services;

namespace StockSense.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IStockService _stock;

    public ProductsController(IStockService stock)
    {
        _stock = stock;
    }

    private int OwnerId => int.Parse(User.FindFirst(AuthService.UserIdClaim)!.Value);

    /// <summary>List your products, optionally filtered by name or SKU.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> List([FromQuery] string? search)
    {
        var products = await _stock.ListProductsAsync(OwnerId, search);
        return Ok(products.Select(ProductResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Get(int id)
    {
        var product = await _stock.GetProductAsync(OwnerId, id);
        return product is null ? NotFound() : Ok(ProductResponse.From(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        var result = await _stock.CreateProductAsync(OwnerId, request);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { error = result.Error });
        }

        var response = ProductResponse.From(result.Value!);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request)
    {
        var result = await _stock.UpdateProductAsync(OwnerId, id, request);
        return result.Success
            ? Ok(ProductResponse.From(result.Value!))
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _stock.DeleteProductAsync(OwnerId, id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Record stock received (positive change) or sold / lost (negative change).</summary>
    [HttpPost("{id:int}/adjust")]
    public async Task<ActionResult<ProductResponse>> Adjust(int id, StockAdjustmentRequest request)
    {
        var result = await _stock.AdjustStockAsync(OwnerId, id, request);
        return result.Success
            ? Ok(ProductResponse.From(result.Value!))
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>The last 100 stock movements for a product.</summary>
    [HttpGet("{id:int}/movements")]
    public async Task<ActionResult<IEnumerable<StockMovementResponse>>> Movements(int id)
    {
        var movements = await _stock.GetMovementsAsync(OwnerId, id);
        if (movements is null)
        {
            return NotFound();
        }

        return Ok(movements.Select(m => new StockMovementResponse
        {
            Id = m.Id,
            Change = m.Change,
            Reason = m.Reason,
            CreatedAt = m.CreatedAt
        }));
    }
}
