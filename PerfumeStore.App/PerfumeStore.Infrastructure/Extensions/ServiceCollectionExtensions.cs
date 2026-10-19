using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerfumeStore.Application.Dashboard.Interfaces;
using PerfumeStore.Domain.Repositories;
using PerfumeStore.Infrastructure.Persistence;
using PerfumeStore.Infrastructure.Repositories;

namespace PerfumeStore.Infrastructure.Extensions {
    public static class ServiceCollectionExtensions {

        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
            var connectionString = configuration.GetConnectionString("PerfumeStore");
            services.AddDbContext<PerfumeStoreDbContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped<IBrandsRepository, BrandsRepository>();
            services.AddScoped<IProductsRepository, ProductsRepository>();
            services.AddScoped<IProductCategoriesRepository, ProductCategoriesRepository>();
            services.AddScoped<IPersonsRepository, PersonsRepository>();
            services.AddScoped<ICustomersRepository, CustomersRepository>();
            services.AddScoped<ISuppliersRepository, SuppliersRepository>();
            services.AddScoped<IInventoryRepository, InventoryRepository>();
            services.AddScoped<IMoneyAccountsRepository, MoneyAccountsRepository>();
            services.AddScoped<IMoneyTransactionsRepository, MoneyTransactionsRepository>();
            services.AddScoped<ISalesInvoicesRepository, SalesInvoicesRepository>();
            services.AddScoped<IPurchaseInvoicesRepository, PurchaseInvoicesRepository>();
            services.AddScoped<IPaymentVouchersRepository, PaymentVouchersRepository>();
            services.AddScoped<IReceiptVouchersRepository, ReceiptVouchersRepository>();
            services.AddScoped<IDebtsRepository, DebtsRepository>();
            services.AddScoped<IExpensesRepository, ExpensesRepository>();
            services.AddScoped<IExpenseTypesRepository, ExpenseTypesRepository>();
            services.AddScoped<IDashboardService, DashboardRepository>();
            services.AddScoped<PerfumeStore.Application.FinancialHistory.IFinancialHistoryService, FinancialHistoryRepository>();
        }

    }
}
