using MoneyPal.Models;

namespace MoneyPal.Services;

public class TransactionService : ITransactionService
{
    private readonly DataStorageService _storage;

    public TransactionService(IDataStorageService storage)
    {
        _storage = (DataStorageService)storage;
    }

    public async Task<Expense> AddExpenseAsync(Expense expense)
    {
        return await _storage.InsertExpenseItemAsync(expense);
    }

    public async Task<Expense> UpdateExpenseAsync(Expense expense)
    {
        await _storage.UpdateExpenseItemAsync(expense);
        return expense;
    }

    public async Task<bool> DeleteExpenseAsync(Guid expenseId)
    {
        var result = await _storage.DeleteExpenseItemAsync(expenseId);
        return result > 0;
    }

    public async Task<Expense?> GetExpenseByIdAsync(Guid expenseId)
    {
        return await _storage.GetExpenseItemByIdAsync(expenseId);
    }

    public async Task<List<Expense>> GetExpensesForBudgetAsync(Guid budgetId, int month, int year)
    {
        return await _storage.GetExpensesForBudgetAsync(budgetId, month, year);
    }

    public async Task<List<Expense>> GetAllExpensesForMonthAsync(int month, int year)
    {
        return await _storage.GetAllExpensesForMonthAsync(month, year);
    }

    public async Task<decimal> GetTotalForBudgetAsync(Guid budgetId, int month, int year)
    {
        return await _storage.GetTotalExpensesForBudgetAsync(budgetId, month, year);
    }

    public async Task<List<Expense>> GetOneTimeExpensesForMonthAsync(int month, int year)
    {
        return await _storage.GetOneTimeExpensesForMonthAsync(month, year);
    }

    public async Task<decimal> GetTotalOneTimeExpensesForMonthAsync(int month, int year)
    {
        return await _storage.GetTotalOneTimeExpensesForMonthAsync(month, year);
    }
}
