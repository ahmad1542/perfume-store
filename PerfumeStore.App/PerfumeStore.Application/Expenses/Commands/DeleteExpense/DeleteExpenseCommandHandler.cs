using MediatR;
using PerfumeStore.Domain.Entities;
using PerfumeStore.Domain.Exceptions;
using PerfumeStore.Domain.Repositories;

namespace PerfumeStore.Application.Expenses.Commands.DeleteExpense;

public class DeleteExpenseCommandHandler(IExpensesRepository expenses, IMoneyAccountsRepository accounts)
    : IRequestHandler<DeleteExpenseCommand> {
    public async Task Handle(DeleteExpenseCommand request, CancellationToken cancellationToken) {
        var expense = await expenses.GetByIdAsync(request.ID)
            ?? throw new NotFoundException(nameof(Expense), request.ID.ToString());
        var account = await accounts.GetByIdAsync(expense.MoneyAccountID)
            ?? throw new NotFoundException(nameof(MoneyAccount), expense.MoneyAccountID.ToString());
        // The repository saves the deletion and the tracked balance together;
        // financial history shares the same transaction and retains the reversal.
        account.CurrentBalance += expense.Amount;
        await expenses.DeleteAsync(expense);
    }
}
