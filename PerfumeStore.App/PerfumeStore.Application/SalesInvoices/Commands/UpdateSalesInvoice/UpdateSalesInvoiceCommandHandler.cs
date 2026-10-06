using MediatR;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.SalesInvoices.Commands.UpdateSalesInvoice {
    public class UpdateSalesInvoiceCommandHandler(ISalesInvoicesRepository salesInvoicesRepository,
        IDebtsRepository debtsRepository,
        IMoneyAccountsRepository moneyAccountsRepository,
        IInventoryRepository inventoryRepository) : IRequestHandler<UpdateSalesInvoiceCommand> {
        public async Task Handle(UpdateSalesInvoiceCommand request, CancellationToken cancellationToken) {

            var salesInvoice = await salesInvoicesRepository.GetByIdAsync(request.ID);

            if (salesInvoice == null)
                throw new NotFoundException(nameof(SalesInvoice), request.ID.ToString());

            if (request.HasDebt && request.DebtAmount.HasValue && request.DebtAmount.Value > 0 && !request.CustomerId.HasValue)
                throw new BusinessRuleException("Customer is required when creating or updating a debt for a sales invoice.");

            foreach (KeyValuePair<int, int> kvp in request.Products) {
                var inventory = await inventoryRepository.GetByIdAsync(kvp.Key);
                if (inventory != null) {
                    var available = inventory.Quantity + salesInvoice.SalesInvoiceItems
                        .Where(i => i.ProductID == kvp.Key).Sum(i => i.Quantity);
                    if (available < kvp.Value)
                        throw new BusinessRuleException($"Not enough stock for product id {kvp.Key}. Available after undoing this sale: {available}, requested: {kvp.Value}");
                } else {
                    throw new NotFoundException(nameof(Domain.Entities.Inventory), kvp.Key.ToString());
                }
            }

            var oldAmountPaid = salesInvoice.AmountPaid;
            var oldMoneyAccountId = salesInvoice.MoneyAccountId;

            MoneyAccount? oldAccount = null;
            MoneyAccount? newAccount = null;

            if (request.AmountPaid > 0) {
                if (!request.MoneyAccountId.HasValue)
                    throw new Exception("Money account is required when amount is paid.");

                newAccount = await moneyAccountsRepository.GetByIdAsync(request.MoneyAccountId.Value);

                if (newAccount == null)
                    throw new Exception("Invalid money account.");
            } else if (request.MoneyAccountId.HasValue) {
                throw new Exception("Money account should only be selected when amount paid is greater than zero.");
            }

            if (oldAmountPaid > 0 && oldMoneyAccountId.HasValue) {
                oldAccount = await moneyAccountsRepository.GetByIdAsync(oldMoneyAccountId.Value);

                if (oldAccount == null)
                    throw new Exception("Old money account not found.");
            }

            if (oldAccount != null) {
                oldAccount.CurrentBalance -= oldAmountPaid;
            }

            salesInvoice.Date = request.Date;
            salesInvoice.Notes = request.Notes;
            salesInvoice.CustomerId = request.CustomerId;
            salesInvoice.AmountPaid = request.AmountPaid;
            salesInvoice.MoneyAccountId = request.AmountPaid > 0 ? request.MoneyAccountId : null;

            if (newAccount != null) {
                newAccount.CurrentBalance += request.AmountPaid;
            }

            if (request.HasDebt && request.DebtAmount.HasValue && request.DebtAmount.Value > 0) {
                if (salesInvoice.Debt != null) {
                    salesInvoice.Debt.Date = request.Date;
                    salesInvoice.Debt.Amount = request.DebtAmount.Value;
                    salesInvoice.Debt.Notes = request.DebtNotes;
                    salesInvoice.Debt.PersonId = request.CustomerId;
                    salesInvoice.Debt.Direction = DebtDirection.Receivable;
                    salesInvoice.Debt.SalesInvoiceId = salesInvoice.ID;
                } else {
                    salesInvoice.Debt = new Debt {
                        Date = request.Date,
                        Amount = request.DebtAmount.Value,
                        Notes = request.DebtNotes,
                        PersonId = request.CustomerId,
                        Direction = DebtDirection.Receivable,
                        SalesInvoiceId = salesInvoice.ID
                    };
                }
            } else {
                if (salesInvoice.Debt != null) {
                    await debtsRepository.SoftDeleteAsync(salesInvoice.Debt);
                    salesInvoice.Debt = null;
                }
            }

            await salesInvoicesRepository.UpdateProductsAsync(salesInvoice.ID, request.Products, request.UnitPrices);
        }
    }
}
