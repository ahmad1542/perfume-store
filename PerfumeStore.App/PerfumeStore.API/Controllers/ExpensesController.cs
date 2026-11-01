using PerfumeStore.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.Expenses.Commands.CreateExpense;
using PerfumeStore.Application.Expenses.Commands.DeleteExpense;
using PerfumeStore.Application.Expenses.Commands.UpdateExpense;
using PerfumeStore.Application.Expenses.Dtos;
using PerfumeStore.Application.Expenses.Queries.GetAllExpenses;
using PerfumeStore.Application.Expenses.Queries.GetExpenseById;

namespace PerfumeStore.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class ExpensesController(IMediator mediator) : ControllerBase {

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseDto?>> GetById([FromRoute] long id) {
            var expense = await mediator.Send(new GetExpenseByIdQuery(id));
            return Ok(expense);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetAll([FromQuery] RecordFilters filters, [FromQuery] string? search, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] List<long>? expenseTypeIds) {
            var expenses = await mediator.Send(new GetAllExpensesQuery { Filters = filters,
                Search = search,
                FromDate = fromDate,
                ToDate = toDate,
                ExpenseTypeIds = expenseTypeIds
            });
            return Ok(expenses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseCommand command) {
            var id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateExpense([FromRoute] long id, [FromBody] UpdateExpenseCommand command) {
            command.ID = id;
            await mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteExpense(long id) {
            await mediator.Send(new DeleteExpenseCommand(id));
            return NoContent();
        }
    }
}
