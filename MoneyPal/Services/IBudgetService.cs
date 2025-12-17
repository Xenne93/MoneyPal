using MoneyPal.Models;

namespace MoneyPal.Services;

public interface IBudgetService
{
    Task<List<Budget>> GetAllBudgetsAsync();
    Task<Budget?> GetBudgetByIdAsync(Guid id);
    Task<Budget> AddBudgetAsync(Budget budget);
    Task<Budget> UpdateBudgetAsync(Budget budget);
    Task<bool> DeleteBudgetAsync(Guid id);
    Task<decimal> GetTotalBudgetAsync();
    Task<BudgetSpending> UpdateBudgetSpendingAsync(Guid budgetId, int month, int year, decimal amountSpent);
    Task<decimal> GetBudgetSpentAsync(Guid budgetId, int month, int year);
    Task<List<BudgetSpending>> GetBudgetSpendingsForMonthAsync(int month, int year);
}
