using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSense.Api.Dtos;
using StockSense.Api.Services;

namespace StockSense.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IStockService _stock;

    public DashboardController(IStockService stock)
    {
        _stock = stock;
    }

    private int OwnerId => int.Parse(User.FindFirst(AuthService.UserIdClaim)!.Value);

    /// <summary>Headline numbers: product count, units, stock value, low-stock count.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> Summary() =>
        Ok(await _stock.GetSummaryAsync(OwnerId));

    /// <summary>Products at or below their reorder level, with a suggested order quantity.</summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<LowStockItem>>> LowStock() =>
        Ok(await _stock.GetLowStockAsync(OwnerId));
}
