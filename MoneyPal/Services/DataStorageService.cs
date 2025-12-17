using SQLite;
using MoneyPal.Models;

namespace MoneyPal.Services;

public class DataStorageService : IDataStorageService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private bool _isInitialized;

    public DataStorageService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "moneypal.db3");
        _database = new SQLiteAsyncConnection(dbPath);
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await _initSemaphore.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            // Migration: Drop and recreate BankBalance table due to schema change
            try
            {
                await _database.DropTableAsync<BankBalance>();
            }
            catch
            {
                // Table might not exist yet, ignore error
            }

            // Create tables
            await _database.CreateTableAsync<RecurringExpense>();
            await _database.CreateTableAsync<Category>();
            await _database.CreateTableAsync<PaymentRecord>();
            await _database.CreateTableAsync<Budget>();
            await _database.CreateTableAsync<BudgetSpending>();
            await _database.CreateTableAsync<Expense>();
            await _database.CreateTableAsync<Income>();
            await _database.CreateTableAsync<BankBalance>();

            _isInitialized = true;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task<T?> LoadAsync<T>(string key) where T : class
    {
        try
        {
            await InitializeAsync();

            if (typeof(T) == typeof(List<RecurringExpense>))
            {
                var expenses = await _database.Table<RecurringExpense>().ToListAsync();
                return expenses as T;
            }
            else if (typeof(T) == typeof(List<Category>))
            {
                var categories = await _database.Table<Category>().ToListAsync();
                return categories as T;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data for {key}: {ex.Message}");
            return null;
        }
    }

    public async Task SaveAsync<T>(string key, T data) where T : class
    {
        try
        {
            await InitializeAsync();

            if (data is List<RecurringExpense> expenses)
            {
                // Delete all existing and insert new ones (simple approach)
                await _database.DeleteAllAsync<RecurringExpense>();
                await _database.InsertAllAsync(expenses);
            }
            else if (data is List<Category> categories)
            {
                await _database.DeleteAllAsync<Category>();
                await _database.InsertAllAsync(categories);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving data for {key}: {ex.Message}");
        }
    }

    public async Task DeleteAsync(string key)
    {
        try
        {
            await InitializeAsync();

            if (key == "expenses")
            {
                await _database.DeleteAllAsync<RecurringExpense>();
            }
            else if (key == "categories")
            {
                await _database.DeleteAllAsync<Category>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting data for {key}: {ex.Message}");
        }
    }

    // Direct database access methods for better performance
    public async Task<RecurringExpense> InsertExpenseAsync(RecurringExpense expense)
    {
        await InitializeAsync();
        await _database.InsertAsync(expense);
        return expense;
    }

    public async Task<int> UpdateExpenseAsync(RecurringExpense expense)
    {
        await InitializeAsync();
        return await _database.UpdateAsync(expense);
    }

    public async Task<int> DeleteExpenseAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.DeleteAsync<RecurringExpense>(id);
    }

    public async Task<RecurringExpense?> GetExpenseByIdAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.Table<RecurringExpense>()
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<RecurringExpense>> GetAllExpensesAsync()
    {
        await InitializeAsync();
        return await _database.Table<RecurringExpense>()
            .OrderBy(e => e.DayOfMonth)
            .ToListAsync();
    }

    public async Task<Category> InsertCategoryAsync(Category category)
    {
        await InitializeAsync();
        await _database.InsertAsync(category);
        return category;
    }

    public async Task<int> DeleteCategoryAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.DeleteAsync<Category>(id);
    }

    public async Task<Category?> GetCategoryByIdAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.Table<Category>()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        await InitializeAsync();
        return await _database.Table<Category>()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalMonthlyExpensesAsync()
    {
        await InitializeAsync();
        var expenses = await _database.Table<RecurringExpense>()
            .Where(e => e.IsActive)
            .ToListAsync();
        return expenses.Sum(e => e.Amount);
    }

    // Payment tracking methods
    public async Task<PaymentRecord?> GetPaymentRecordAsync(Guid expenseId, int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<PaymentRecord>()
            .Where(p => p.ExpenseId == expenseId && p.Month == month && p.Year == year)
            .FirstOrDefaultAsync();
    }

    public async Task<List<PaymentRecord>> GetPaymentRecordsForMonthAsync(int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<PaymentRecord>()
            .Where(p => p.Month == month && p.Year == year)
            .ToListAsync();
    }

    public async Task<PaymentRecord> UpsertPaymentRecordAsync(PaymentRecord record)
    {
        await InitializeAsync();

        var existing = await GetPaymentRecordAsync(record.ExpenseId, record.Month, record.Year);
        if (existing != null)
        {
            record.Id = existing.Id;
            record.ModifiedAt = DateTime.UtcNow;
            await _database.UpdateAsync(record);
        }
        else
        {
            record.Id = Guid.NewGuid();
            record.CreatedAt = DateTime.UtcNow;
            await _database.InsertAsync(record);
        }

        return record;
    }

    public async Task<int> DeletePaymentRecordAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.DeleteAsync<PaymentRecord>(id);
    }

    // Budget methods
    public async Task<Budget> InsertBudgetAsync(Budget budget)
    {
        await InitializeAsync();
        await _database.InsertAsync(budget);
        return budget;
    }

    public async Task<int> UpdateBudgetAsync(Budget budget)
    {
        await InitializeAsync();
        return await _database.UpdateAsync(budget);
    }

    public async Task<int> DeleteBudgetAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.DeleteAsync<Budget>(id);
    }

    public async Task<Budget?> GetBudgetByIdAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.Table<Budget>()
            .Where(b => b.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Budget>> GetAllBudgetsAsync()
    {
        await InitializeAsync();
        return await _database.Table<Budget>()
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    // Budget spending methods
    public async Task<BudgetSpending?> GetBudgetSpendingAsync(Guid budgetId, int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<BudgetSpending>()
            .Where(bs => bs.BudgetId == budgetId && bs.Month == month && bs.Year == year)
            .FirstOrDefaultAsync();
    }

    public async Task<List<BudgetSpending>> GetBudgetSpendingsForMonthAsync(int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<BudgetSpending>()
            .Where(bs => bs.Month == month && bs.Year == year)
            .ToListAsync();
    }

    public async Task<BudgetSpending> UpsertBudgetSpendingAsync(BudgetSpending spending)
    {
        await InitializeAsync();

        var existing = await GetBudgetSpendingAsync(spending.BudgetId, spending.Month, spending.Year);
        if (existing != null)
        {
            spending.Id = existing.Id;
            spending.ModifiedAt = DateTime.UtcNow;
            await _database.UpdateAsync(spending);
        }
        else
        {
            spending.Id = Guid.NewGuid();
            spending.CreatedAt = DateTime.UtcNow;
            await _database.InsertAsync(spending);
        }

        return spending;
    }

    // Expense methods (individual transactions)
    public async Task<Expense> InsertExpenseItemAsync(Expense expense)
    {
        await InitializeAsync();
        await _database.InsertAsync(expense);
        return expense;
    }

    public async Task<int> UpdateExpenseItemAsync(Expense expense)
    {
        await InitializeAsync();
        expense.ModifiedAt = DateTime.UtcNow;
        return await _database.UpdateAsync(expense);
    }

    public async Task<int> DeleteExpenseItemAsync(Guid id)
    {
        await InitializeAsync();
        var expense = await GetExpenseItemByIdAsync(id);
        if (expense != null)
        {
            expense.IsDeleted = true;
            expense.ModifiedAt = DateTime.UtcNow;
            return await _database.UpdateAsync(expense);
        }
        return 0;
    }

    public async Task<Expense?> GetExpenseItemByIdAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.Table<Expense>()
            .Where(e => e.Id == id && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Expense>> GetExpensesForBudgetAsync(Guid budgetId, int month, int year)
    {
        await InitializeAsync();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _database.Table<Expense>()
            .Where(e => e.BudgetId == budgetId &&
                       e.Date >= startDate &&
                       e.Date <= endDate &&
                       !e.IsDeleted)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<List<Expense>> GetAllExpensesForMonthAsync(int month, int year)
    {
        await InitializeAsync();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _database.Table<Expense>()
            .Where(e => e.Date >= startDate &&
                       e.Date <= endDate &&
                       !e.IsDeleted)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalExpensesForBudgetAsync(Guid budgetId, int month, int year)
    {
        var expenses = await GetExpensesForBudgetAsync(budgetId, month, year);
        return expenses.Sum(e => e.Amount);
    }

    public async Task<List<Expense>> GetOneTimeExpensesForMonthAsync(int month, int year)
    {
        await InitializeAsync();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _database.Table<Expense>()
            .Where(e => e.BudgetId == null &&
                       e.Date >= startDate &&
                       e.Date <= endDate &&
                       !e.IsDeleted)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalOneTimeExpensesForMonthAsync(int month, int year)
    {
        var expenses = await GetOneTimeExpensesForMonthAsync(month, year);
        return expenses.Sum(e => e.Amount);
    }

    // Income methods
    public async Task<Income> InsertIncomeAsync(Income income)
    {
        await InitializeAsync();
        await _database.InsertAsync(income);
        return income;
    }

    public async Task<int> UpdateIncomeAsync(Income income)
    {
        await InitializeAsync();
        income.ModifiedAt = DateTime.UtcNow;
        return await _database.UpdateAsync(income);
    }

    public async Task<int> DeleteIncomeAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.DeleteAsync<Income>(id);
    }

    public async Task<Income?> GetIncomeByIdAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.Table<Income>()
            .Where(i => i.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Income>> GetAllIncomesAsync()
    {
        await InitializeAsync();
        return await _database.Table<Income>()
            .OrderBy(i => i.DayOfMonth)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalMonthlyIncomesAsync()
    {
        await InitializeAsync();
        var incomes = await _database.Table<Income>()
            .Where(i => i.IsActive)
            .ToListAsync();
        return incomes.Sum(i => i.Amount);
    }

    // Bank Balance methods
    public async Task<BankBalance> GetBankBalanceAsync(int month, int year)
    {
        await InitializeAsync();
        var balance = await _database.Table<BankBalance>()
            .Where(b => b.Month == month && b.Year == year)
            .FirstOrDefaultAsync();

        if (balance == null)
        {
            // Create initial balance of 0 for this month
            balance = new BankBalance
            {
                Id = Guid.NewGuid(),
                Month = month,
                Year = year,
                CurrentBalance = 0,
                LastUpdated = DateTime.UtcNow
            };
            await _database.InsertAsync(balance);
        }

        return balance;
    }

    public async Task<BankBalance> UpdateBankBalanceAsync(int month, int year, decimal newBalance)
    {
        await InitializeAsync();
        var balance = await GetBankBalanceAsync(month, year);
        balance.CurrentBalance = newBalance;
        balance.LastUpdated = DateTime.UtcNow;
        await _database.UpdateAsync(balance);
        return balance;
    }
}
