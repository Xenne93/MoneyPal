using MoneyPal.Models;

namespace MoneyPal.Services;

public class BudgetService : IBudgetService
{
    private readonly IDataStorageService _storage;

    public BudgetService(IDataStorageService storage)
    {
        _storage = storage;
    }

    public async Task<List<Budget>> GetAllBudgetsAsync()
    {
        return await ((DataStorageService)_storage).GetAllBudgetsAsync();
    }

    public async Task<Budget?> GetBudgetByIdAsync(Guid id)
    {
        return await ((DataStorageService)_storage).GetBudgetByIdAsync(id);
    }

    public async Task<Budget> AddBudgetAsync(Budget budget)
    {
        budget.Id = Guid.NewGuid();
        budget.CreatedAt = DateTime.UtcNow;
        return await ((DataStorageService)_storage).InsertBudgetAsync(budget);
    }

    public async Task<Budget> UpdateBudgetAsync(Budget budget)
    {
        budget.ModifiedAt = DateTime.UtcNow;
        await ((DataStorageService)_storage).UpdateBudgetAsync(budget);
        return budget;
    }

    public async Task<bool> DeleteBudgetAsync(Guid id)
    {
        var result = await ((DataStorageService)_storage).DeleteBudgetAsync(id);
        return result > 0;
    }

    public async Task<decimal> GetTotalBudgetAsync()
    {
        var budgets = await GetAllBudgetsAsync();
        return budgets.Where(b => b.IsActive).Sum(b => b.Amount);
    }

    public async Task<BudgetSpending> UpdateBudgetSpendingAsync(Guid budgetId, int month, int year, decimal amountSpent)
    {
        var spending = new BudgetSpending
        {
            BudgetId = budgetId,
            Month = month,
            Year = year,
            AmountSpent = amountSpent
        };

        return await ((DataStorageService)_storage).UpsertBudgetSpendingAsync(spending);
    }

    public async Task<decimal> GetBudgetSpentAsync(Guid budgetId, int month, int year)
    {
        var spending = await ((DataStorageService)_storage).GetBudgetSpendingAsync(budgetId, month, year);
        return spending?.AmountSpent ?? 0;
    }

    public async Task<List<BudgetSpending>> GetBudgetSpendingsForMonthAsync(int month, int year)
    {
        return await ((DataStorageService)_storage).GetBudgetSpendingsForMonthAsync(month, year);
    }
}
