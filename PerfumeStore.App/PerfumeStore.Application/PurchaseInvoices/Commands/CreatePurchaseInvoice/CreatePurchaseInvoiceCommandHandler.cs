using PerfumeStore.Application.Accounting;
﻿using AutoMapper;
using MediatR;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.PurchaseInvoices.Commands.CreatePurchaseInvoice {
    public class CreatePurchaseInvoiceCommandHandler(IPurchaseInvoicesRepository purchaseInvoicesRepository, IMoneyAccountsRepository moneyAccountsRepository, IMapper mapper, IProductsRepository productsRepository) : IRequestHandler<CreatePurchaseInvoiceCommand, long> {
        public async Task<long> Handle(CreatePurchaseInvoiceCommand request, CancellationToken cancellationToken) {
            await InvoiceSettlement.EnsureAsync(productsRepository, request.Products, request.UnitCosts, true, request.AmountPaid, request.HasDebt, request.DebtAmount);
            var purchaseInvoice = mapper.Map<PurchaseInvoice>(request);

            MoneyAccount? account = null;

            if (request.AmountPaid > 0) {
                if (!request.MoneyAccountId.HasValue)
                    throw new Exception("Money account is required when amount is paid.");

                account = await moneyAccountsRepository.GetByIdAsync(request.MoneyAccountId.Value);

                if (account == null)
                    throw new Exception("Invalid money account.");
            }

            await purchaseInvoicesRepository.AddAsync(purchaseInvoice, request.Products, request.UnitCosts);

            if (request.AmountPaid > 0) {
                account!.CurrentBalance -= request.AmountPaid;
                await moneyAccountsRepository.SaveChangesAsync();
            }

            // await debtsRepository.AddAsync(purchaseInvoice.Debt!);
            return purchaseInvoice.ID;
        }
    }
}
