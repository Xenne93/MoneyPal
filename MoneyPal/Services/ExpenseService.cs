using MoneyPal.Models;

namespace MoneyPal.Services;

public class ExpenseService : IExpenseService
{
    private readonly IDataStorageService _storage;
    private bool _isInitialized;

    public ExpenseService(IDataStorageService storage)
    {
        _storage = storage;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
            return;

        // Check if categories exist, if not, create defaults
        var categories = await _storage.LoadAsync<List<Category>>("categories");
        if (categories == null || !categories.Any())
        {
            var defaultCategories = Category.GetDefaultCategories();
            foreach (var category in defaultCategories)
            {
                await ((DataStorageService)_storage).InsertCategoryAsync(category);
            }
        }

        _isInitialized = true;
    }

    public async Task<List<RecurringExpense>> GetAllExpensesAsync()
    {
        await EnsureInitializedAsync();
        return await ((DataStorageService)_storage).GetAllExpensesAsync();
    }

    public async Task<RecurringExpense?> GetExpenseByIdAsync(Guid id)
    {
        await EnsureInitializedAsync();
        return await ((DataStorageService)_storage).GetExpenseByIdAsync(id);
    }

    public async Task<RecurringExpense> AddExpenseAsync(RecurringExpense expense)
    {
        await EnsureInitializedAsync();
        expense.Id = Guid.NewGuid();
        expense.CreatedAt = DateTime.UtcNow;
        return await ((DataStorageService)_storage).InsertExpenseAsync(expense);
    }

    public async Task<RecurringExpense> UpdateExpenseAsync(RecurringExpense expense)
    {
        await EnsureInitializedAsync();
        expense.ModifiedAt = DateTime.UtcNow;
        await ((DataStorageService)_storage).UpdateExpenseAsync(expense);
        return expense;
    }

    public async Task<bool> DeleteExpenseAsync(Guid id)
    {
        await EnsureInitializedAsync();
        var result = await ((DataStorageService)_storage).DeleteExpenseAsync(id);
        return result > 0;
    }

    public async Task<decimal> GetTotalMonthlyExpensesAsync()
    {
        await EnsureInitializedAsync();
        return await ((DataStorageService)_storage).GetTotalMonthlyExpensesAsync();
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        await EnsureInitializedAsync();
        return await ((DataStorageService)_storage).GetAllCategoriesAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(Guid id)
    {
        await EnsureInitializedAsync();
        return await ((DataStorageService)_storage).GetCategoryByIdAsync(id);
    }

    public async Task<Category> AddCategoryAsync(Category category)
    {
        await EnsureInitializedAsync();
        category.Id = Guid.NewGuid();
        category.CreatedAt = DateTime.UtcNow;
        return await ((DataStorageService)_storage).InsertCategoryAsync(category);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        await EnsureInitializedAsync();

        var category = await GetCategoryByIdAsync(id);
        if (category != null && !category.IsDefault)
        {
            // Check if any expenses use this category
            var allExpenses = await GetAllExpensesAsync();
            var hasExpenses = allExpenses.Any(e => e.CategoryId == id);

            if (!hasExpenses)
            {
                var result = await ((DataStorageService)_storage).DeleteCategoryAsync(id);
                return result > 0;
            }
        }
        return false;
    }
}
