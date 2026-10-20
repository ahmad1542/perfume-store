using MediatR;

namespace PerfumeStore.Application.Expenses.Commands.DeleteExpense;

public record DeleteExpenseCommand(long ID) : IRequest;
