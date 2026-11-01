using PerfumeStore.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.ReceiptVouchers.Commands.CreateReceiptVoucher;
using PerfumeStore.Application.ReceiptVouchers.Dtos;
using PerfumeStore.Application.ReceiptVouchers.Queries.GetAllReceiptVouchers;
using PerfumeStore.Application.ReceiptVouchers.Queries.GetOpenPersonDebts;
using PerfumeStore.Application.ReceiptVouchers.Queries.GetOpenSalesInvoices;
using PerfumeStore.Application.ReceiptVouchers.Queries.GetReceiptVoucherById;

namespace PerfumeStore.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReceiptVouchersController(IMediator mediator) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReceiptVoucherDto>>> GetAll([FromQuery] RecordFilters filters, [FromQuery] string? search, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate) {
        var items = await mediator.Send(new GetAllReceiptVouchersQuery { Filters = filters, Search = search, FromDate = fromDate, ToDate = toDate });
        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ReceiptVoucherDetailsDto?>> GetById([FromRoute] long id) {
        var item = await mediator.Send(new GetReceiptVoucherByIdQuery(id));
        return Ok(item);
    }

    [HttpGet("open-sales-invoices/{customerId:guid}")]
    public async Task<ActionResult<IEnumerable<OpenSalesInvoiceDto>>> GetOpenSalesInvoices([FromRoute] Guid customerId) {
        var items = await mediator.Send(new GetOpenSalesInvoicesQuery(customerId));
        return Ok(items);
    }

    [HttpGet("open-person-debts/{personId:guid}")]
    public async Task<ActionResult<IEnumerable<OpenPersonDebtDto>>> GetOpenPersonDebts([FromRoute] Guid personId) {
        var items = await mediator.Send(new GetOpenPersonDebtsQuery(personId));
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReceiptVoucherCommand command) {
        var id = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }
}
