using MoneyPal.Models;

namespace MoneyPal.Services;

public interface IExpenseService
{
    Task<List<RecurringExpense>> GetAllExpensesAsync();
    Task<RecurringExpense?> GetExpenseByIdAsync(Guid id);
    Task<RecurringExpense> AddExpenseAsync(RecurringExpense expense);
    Task<RecurringExpense> UpdateExpenseAsync(RecurringExpense expense);
    Task<bool> DeleteExpenseAsync(Guid id);
    Task<decimal> GetTotalMonthlyExpensesAsync();

    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(Guid id);
    Task<Category> AddCategoryAsync(Category category);
    Task<bool> DeleteCategoryAsync(Guid id);
}
