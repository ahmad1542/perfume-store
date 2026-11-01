using PerfumeStore.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Application.PaymentVouchers.Commands.CreatePaymentVoucher;
using PerfumeStore.Application.PaymentVouchers.Dtos;
using PerfumeStore.Application.PaymentVouchers.Queries.GetAllPaymentVouchers;
using PerfumeStore.Application.PaymentVouchers.Queries.GetOpenPurchaseInvoices;
using PerfumeStore.Application.PaymentVouchers.Queries.GetPaymentVoucherById;

namespace PerfumeStore.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentVouchersController(IMediator mediator) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentVoucherDto>>> GetAll([FromQuery] RecordFilters filters, [FromQuery] string? search, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate) {
        var items = await mediator.Send(new GetAllPaymentVouchersQuery { Filters = filters, Search = search, FromDate = fromDate, ToDate = toDate });
        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PaymentVoucherDetailsDto?>> GetById([FromRoute] long id) {
        var item = await mediator.Send(new GetPaymentVoucherByIdQuery(id));
        return Ok(item);
    }

    [HttpGet("open-purchase-invoices/{supplierId:guid}")]
    public async Task<ActionResult<IEnumerable<OpenPurchaseInvoiceDto>>> GetOpenPurchaseInvoices([FromRoute] Guid supplierId) {
        var items = await mediator.Send(new GetOpenPurchaseInvoicesQuery(supplierId));
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentVoucherCommand command) {
        var id = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }
}
