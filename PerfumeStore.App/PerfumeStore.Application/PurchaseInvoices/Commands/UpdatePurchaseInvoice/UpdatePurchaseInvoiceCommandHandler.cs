using MediatR;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.PurchaseInvoices.Commands.UpdatePurchaseInvoice {
    public class UpdatePurchaseInvoiceCommandHandler(IPurchaseInvoicesRepository purchaseInvoicesRepository, IDebtsRepository debtsRepository, IMoneyAccountsRepository moneyAccountsRepository) : IRequestHandler<UpdatePurchaseInvoiceCommand> {
        public async Task Handle(UpdatePurchaseInvoiceCommand request, CancellationToken cancellationToken) {

            var purchaseInvoice = await purchaseInvoicesRepository.GetByIdAsync(request.ID);

            if (purchaseInvoice == null)
                throw new NotFoundException(nameof(PurchaseInvoice), request.ID.ToString());

            var oldAmountPaid = purchaseInvoice.AmountPaid;
            var oldMoneyAccountId = purchaseInvoice.MoneyAccountId;

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
                oldAccount.CurrentBalance += oldAmountPaid;
            }

            purchaseInvoice.Date = request.Date;
            purchaseInvoice.Notes = request.Notes;
            purchaseInvoice.SupplierId = request.SupplierId;
            purchaseInvoice.AmountPaid = request.AmountPaid;
            purchaseInvoice.MoneyAccountId = request.AmountPaid > 0 ? request.MoneyAccountId : null;

            if (newAccount != null) {
                newAccount.CurrentBalance -= request.AmountPaid;
            }

            if (request.HasDebt && request.DebtAmount.HasValue && request.DebtAmount.Value > 0) {
                if (purchaseInvoice.Debt != null) {
                    purchaseInvoice.Debt.Date = request.Date;
                    purchaseInvoice.Debt.Amount = request.DebtAmount.Value;
                    purchaseInvoice.Debt.Notes = request.DebtNotes;
                    purchaseInvoice.Debt.PersonId = request.SupplierId;
                    purchaseInvoice.Debt.Direction = DebtDirection.Payable;
                    purchaseInvoice.Debt.PurchaseInvoiceId = purchaseInvoice.ID;
                } else {
                    purchaseInvoice.Debt = new Debt {
                        Date = request.Date,
                        Amount = request.DebtAmount.Value,
                        Notes = request.DebtNotes,
                        PersonId = request.SupplierId,
                        Direction = DebtDirection.Payable,
                        PurchaseInvoiceId = purchaseInvoice.ID
                    };
                }
            } else {
                if (purchaseInvoice.Debt != null) {
                    await debtsRepository.SoftDeleteAsync(purchaseInvoice.Debt);
                    purchaseInvoice.Debt = null;
                }
            }

            await purchaseInvoicesRepository.UpdateProductsAsync(purchaseInvoice.ID, request.Products, request.UnitCosts);
        }
    }
}
