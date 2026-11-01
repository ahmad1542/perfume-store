using PerfumeStore.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.PurchaseInvoices.Commands.CreatePurchaseInvoice;
using PerfumeStore.Application.PurchaseInvoices.Commands.UpdatePurchaseInvoice;
using PerfumeStore.Application.PurchaseInvoices.Dtos;
using PerfumeStore.Application.PurchaseInvoices.Queries.GetAllPurchaseinvoices;
using PerfumeStore.Application.PurchaseInvoices.Queries.GetPurchaseInvoiceById;

namespace PerfumeStore.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseInvoicesController(IMediator mediator) : ControllerBase {

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseInvoiceDetailsDto?>> GetById([FromRoute] long id) {
            var purchaseInvoice = await mediator.Send(new GetPurchaseInvoiceByIdQuery(id));

            return Ok(purchaseInvoice);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseInvoiceDto>>> GetAll([FromQuery] RecordFilters filters,
            [FromQuery] string? search,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate) {
            var purchaseInvoices = await mediator.Send(new GetAllPurchaseinvoicesQuery { Filters = filters,
                Search = search,
                FromDate = fromDate,
                ToDate = toDate
            });

            return Ok(purchaseInvoices);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseInvoice([FromBody] CreatePurchaseInvoiceCommand command) {
            long id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, null);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdatePurchaseInvoice([FromRoute] long id, [FromBody] UpdatePurchaseInvoiceCommand command) {
            command.ID = id;
            await mediator.Send(command);
            return NoContent();
        }
    }
}
