using PerfumeStore.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.MoneyTransactions.Commands.CreateMoneyTransaction;
using PerfumeStore.Application.MoneyTransactions.Commands.UpdateMoneyTransaction;
using PerfumeStore.Application.MoneyTransactions.Dtos;
using PerfumeStore.Application.MoneyTransactions.Queries.GetAllMoneyTransactions;
using PerfumeStore.Application.MoneyTransactions.Queries.GetMoneyTransactionById;

namespace PerfumeStore.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class MoneyTransactionsController(IMediator mediator) : ControllerBase {

        [HttpGet("{id}")]
        public async Task<ActionResult<MoneyTransactionDto?>> GetById([FromRoute] long id) {
            var moneyTransaction = await mediator.Send(new GetMoneyTransactionByIdQuery(id));

            return Ok(moneyTransaction);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MoneyTransactionDto>>> GetAll([FromQuery] RecordFilters filters, [FromQuery] string? search, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate) {
            var moneyAccounts = await mediator.Send(new GetAllMoneyTransactionsQuery(search) { Filters = filters, FromDate = fromDate, ToDate = toDate });
            return Ok(moneyAccounts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMoneyAccount([FromBody] CreateMoneyTransactionCommand command) {
            long id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, null);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateMoneyAccount([FromRoute] long id, [FromBody] UpdateMoneyTransactionCommand command) {
            command.ID = id;
            await mediator.Send(command);
            return NoContent();
        }
    }
}
