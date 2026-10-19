using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.FinancialHistory;

namespace PerfumeStore.API.Controllers;

[ApiController]
[Route("api/FinancialHistory")]
public class FinancialHistoryController(IFinancialHistoryService history) : ControllerBase {
    [HttpGet("{kind}")]
    public async Task<ActionResult<FinancialHistoryDto>> Get(string kind, int? year, int? month, int? accountId, CancellationToken ct) {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem"));
        int selectedYear = year ?? now.Year, selectedMonth = month ?? now.Month;
        if (kind is not ("cash" or "profit") || selectedYear < 2021 || selectedYear > now.Year || selectedMonth < 1 || selectedMonth > 12 || accountId is <= 0)
            return BadRequest(new { detail = "Choose cash or profit, a year from 2021 to the current year, a valid month, and a valid account." });
        return Ok(await history.GetAsync(kind, selectedYear, selectedMonth, accountId, ct));
    }
}
