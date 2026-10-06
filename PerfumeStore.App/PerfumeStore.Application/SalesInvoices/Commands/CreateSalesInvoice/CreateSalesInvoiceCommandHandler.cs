using AutoMapper;
using MediatR;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.SalesInvoices.Commands.CreateSalesInvoice {
    public class CreateSalesInvoiceCommandHandler(ISalesInvoicesRepository salesInvoicesRepository,
        IMoneyAccountsRepository moneyAccountsRepository,
        IInventoryRepository inventoryRepository,
        IMapper mapper) : IRequestHandler<CreateSalesInvoiceCommand, long> {
        public async Task<long> Handle(CreateSalesInvoiceCommand request, CancellationToken cancellationToken) {
            if (request.HasDebt && request.DebtAmount.HasValue && request.DebtAmount.Value > 0 && !request.CustomerId.HasValue)
                throw new BusinessRuleException("Customer is required when creating a debt for a sales invoice.");

            var salesInvoice = mapper.Map<SalesInvoice>(request);

            MoneyAccount? account = null;

            if (request.AmountPaid > 0) {
                if (!request.MoneyAccountId.HasValue)
                    throw new BusinessRuleException("Money account is required when amount is paid.");

                account = await moneyAccountsRepository.GetByIdAsync(request.MoneyAccountId.Value);

                if (account == null)
                    throw new NotFoundException(nameof(MoneyAccount), request.MoneyAccountId.Value.ToString());
            }

            foreach (KeyValuePair<int, int> kvp in request.Products) {
                var inventory = await inventoryRepository.GetByIdAsync(kvp.Key);
                if (inventory != null) {
                    if (inventory.Quantity < kvp.Value)
                        throw new BusinessRuleException($"Not enough stock for product id {kvp.Key}. Available: {inventory.Quantity}, requested: {kvp.Value}");
                } else {
                    throw new NotFoundException(nameof(Domain.Entities.Inventory), kvp.Key.ToString());
                }
            }

            await salesInvoicesRepository.AddAsync(salesInvoice, request.Products, request.UnitPrices);

            if (request.AmountPaid > 0) {
                account!.CurrentBalance += request.AmountPaid;
                await moneyAccountsRepository.SaveChangesAsync();
            }

            // await debtsRepository.AddAsync(salesInvoice.Debt!);
            return salesInvoice.ID;
        }
    }
}
