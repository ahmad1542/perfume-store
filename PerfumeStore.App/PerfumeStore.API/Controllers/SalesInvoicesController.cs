using PerfumeStore.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.SalesInvoices.Commands.CreateSalesInvoice;
using PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice;
using PerfumeStore.Application.SalesInvoices.Dtos;
using PerfumeStore.Application.SalesInvoices.Queries.GetAllSalesinvoices;
using PerfumeStore.Application.SalesInvoices.Queries.GetSalesInvoiceById;

namespace PerfumeStore.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class SalesInvoicesController(IMediator mediator) : ControllerBase {

        [HttpGet("{id}")]
        public async Task<ActionResult<SalesInvoiceDetailsDto?>> GetById([FromRoute] long id) {
            var salesInvoice = await mediator.Send(new GetSalesInvoiceByIdQuery(id));

            return Ok(salesInvoice);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesInvoiceDto>>> GetAll([FromQuery] RecordFilters filters,
            [FromQuery] string? search,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate) {
            var salesInvoices = await mediator.Send(new GetAllSalesinvoicesQuery { Filters = filters,
                Search = search,
                FromDate = fromDate,
                ToDate = toDate
            });

            return Ok(salesInvoices);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSalesInvoice([FromBody] CreateSalesInvoiceCommand command) {
            long id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, null);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateSalesInvoice([FromRoute] long id, [FromBody] UpdateSalesInvoiceCommand command) {
            command.ID = id;
            await mediator.Send(command);
            return NoContent();
        }
    }
}