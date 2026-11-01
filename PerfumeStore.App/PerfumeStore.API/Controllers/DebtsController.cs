using PerfumeStore.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.Debts.Commands.CreateDebt;
using PerfumeStore.Application.Debts.Commands.UpdateDebt;
using PerfumeStore.Application.Debts.Dtos;
using PerfumeStore.Application.Debts.Queries.GetAllDebts;
using PerfumeStore.Application.Debts.Queries.GetDebtById;

namespace PerfumeStore.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class DebtsController(IMediator mediator) : ControllerBase {

        [HttpGet("{id}")]
        public async Task<ActionResult<DebtDto?>> GetById([FromRoute] int id) {
            var debt = await mediator.Send(new GetDebtByIdQuery(id));
            return Ok(debt);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DebtDto>>> GetAll([FromQuery] RecordFilters filters, [FromQuery] string? search, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] bool includeSettled= false) {
            var debts = await mediator.Send(new GetAllDebtsQuery(search, includeSettled) { Filters = filters, FromDate = fromDate, ToDate = toDate });
            return Ok(debts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDebt([FromBody] CreateDebtCommand command) {
            int id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, null);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateDebt([FromRoute] int id, [FromBody] UpdateDebtCommand command) {
            command.Id = id;
            await mediator.Send(command);
            return NoContent();
        }
    }
}