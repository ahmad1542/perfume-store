using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.Dashboard.Dtos;
using PerfumeStore.Application.Dashboard.Queries.GetDashboard;

namespace PerfumeStore.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController(IMediator mediator) : ControllerBase {
        [HttpGet]
        public async Task<ActionResult<DashboardDto>> Get([FromQuery] int? year = null, [FromQuery] int? month = null) {
            if (year.HasValue && (year < 2021 || year > DateTime.Now.Year))
                return BadRequest(new { detail = "Year must be between 2021 and the current year." });
            if (month.HasValue && (month < 1 || month > 12))
                return BadRequest(new { detail = "Month must be between 1 and 12." });
            var dashboard = await mediator.Send(new GetDashboardQuery { Year = year, Month = month });
            return Ok(dashboard);
        }
    }
}
