using MoneyPal.Models;

namespace MoneyPal.Services;

public interface ITransactionService
{
    Task<Expense> AddExpenseAsync(Expense expense);
    Task<Expense> UpdateExpenseAsync(Expense expense);
    Task<bool> DeleteExpenseAsync(Guid expenseId);
    Task<Expense?> GetExpenseByIdAsync(Guid expenseId);
    Task<List<Expense>> GetExpensesForBudgetAsync(Guid budgetId, int month, int year);
    Task<List<Expense>> GetAllExpensesForMonthAsync(int month, int year);
    Task<decimal> GetTotalForBudgetAsync(Guid budgetId, int month, int year);
    Task<List<Expense>> GetOneTimeExpensesForMonthAsync(int month, int year);
    Task<decimal> GetTotalOneTimeExpensesForMonthAsync(int month, int year);
}
