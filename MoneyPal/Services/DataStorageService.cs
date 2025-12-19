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

            Console.WriteLine("Initializing database...");

            // Create tables
            try
            {
                await _database.CreateTableAsync<RecurringExpense>();
                await _database.CreateTableAsync<Category>();
                await _database.CreateTableAsync<PaymentRecord>();
                await _database.CreateTableAsync<IncomeRecord>();
                await _database.CreateTableAsync<Budget>();
                await _database.CreateTableAsync<BudgetSpending>();
                await _database.CreateTableAsync<Expense>();
                await _database.CreateTableAsync<Income>();
                await _database.CreateTableAsync<BankBalance>();

                // Monthly snapshot tables
                await _database.CreateTableAsync<MonthStatus>();
                await _database.CreateTableAsync<MonthlyBudgetSnapshot>();
                await _database.CreateTableAsync<MonthlyRecurringExpenseSnapshot>();
                await _database.CreateTableAsync<MonthlyIncomeSnapshot>();

                Console.WriteLine("Database tables created successfully");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating database tables: {ex}");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in InitializeAsync: {ex}");
            throw;
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

    // IncomeRecord methods
    public async Task<IncomeRecord?> GetIncomeRecordAsync(Guid incomeId, int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<IncomeRecord>()
            .Where(i => i.IncomeId == incomeId && i.Month == month && i.Year == year)
            .FirstOrDefaultAsync();
    }

    public async Task<List<IncomeRecord>> GetIncomeRecordsForMonthAsync(int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<IncomeRecord>()
            .Where(i => i.Month == month && i.Year == year)
            .ToListAsync();
    }

    public async Task<IncomeRecord> UpsertIncomeRecordAsync(IncomeRecord record)
    {
        await InitializeAsync();

        var existing = await GetIncomeRecordAsync(record.IncomeId, record.Month, record.Year);
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

    public async Task<int> DeleteIncomeRecordAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.DeleteAsync<IncomeRecord>(id);
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
        try
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

                try
                {
                    await _database.InsertAsync(balance);
                }
                catch (Exception ex)
                {
                    // If insert fails (e.g., race condition), try to get it again
                    Console.WriteLine($"Error inserting bank balance: {ex.Message}");
                    balance = await _database.Table<BankBalance>()
                        .Where(b => b.Month == month && b.Year == year)
                        .FirstOrDefaultAsync();

                    // If still null, return a default balance without saving
                    if (balance == null)
                    {
                        Console.WriteLine("Could not retrieve bank balance after insert failure, returning default");
                        balance = new BankBalance
                        {
                            Id = Guid.NewGuid(),
                            Month = month,
                            Year = year,
                            CurrentBalance = 0,
                            LastUpdated = DateTime.UtcNow
                        };
                    }
                }
            }

            return balance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetBankBalanceAsync: {ex}");
            // Return a default balance instead of throwing
            return new BankBalance
            {
                Id = Guid.NewGuid(),
                Month = month,
                Year = year,
                CurrentBalance = 0,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    public async Task<BankBalance> UpdateBankBalanceAsync(int month, int year, decimal newBalance)
    {
        await InitializeAsync();

        // Try to get existing balance from database
        var existingBalance = await _database.Table<BankBalance>()
            .Where(b => b.Month == month && b.Year == year)
            .FirstOrDefaultAsync();

        if (existingBalance != null)
        {
            // Update existing record
            existingBalance.CurrentBalance = newBalance;
            existingBalance.LastUpdated = DateTime.UtcNow;
            await _database.UpdateAsync(existingBalance);
            return existingBalance;
        }
        else
        {
            // Insert new record
            var newBankBalance = new BankBalance
            {
                Id = Guid.NewGuid(),
                Month = month,
                Year = year,
                CurrentBalance = newBalance,
                LastUpdated = DateTime.UtcNow
            };
            await _database.InsertAsync(newBankBalance);
            return newBankBalance;
        }
    }

    // Month Status methods
    public async Task<MonthStatus?> GetMonthStatusAsync(int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<MonthStatus>()
            .Where(m => m.Month == month && m.Year == year)
            .FirstOrDefaultAsync();
    }

    public async Task<MonthStatus> UpsertMonthStatusAsync(MonthStatus status)
    {
        await InitializeAsync();

        var existing = await GetMonthStatusAsync(status.Month, status.Year);
        if (existing != null)
        {
            status.Id = existing.Id;
            status.CreatedAt = existing.CreatedAt;
            await _database.UpdateAsync(status);
        }
        else
        {
            status.Id = Guid.NewGuid();
            status.CreatedAt = DateTime.UtcNow;
            await _database.InsertAsync(status);
        }

        return status;
    }

    // Monthly Budget Snapshot methods
    public async Task<List<MonthlyBudgetSnapshot>> GetMonthlyBudgetSnapshotsAsync(int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<MonthlyBudgetSnapshot>()
            .Where(b => b.Month == month && b.Year == year)
            .ToListAsync();
    }

    public async Task<MonthlyBudgetSnapshot> InsertMonthlyBudgetSnapshotAsync(MonthlyBudgetSnapshot snapshot)
    {
        await InitializeAsync();
        await _database.InsertAsync(snapshot);
        return snapshot;
    }

    public async Task<int> DeleteMonthlyBudgetSnapshotsAsync(int month, int year)
    {
        await InitializeAsync();
        var snapshots = await GetMonthlyBudgetSnapshotsAsync(month, year);
        foreach (var snapshot in snapshots)
        {
            await _database.DeleteAsync<MonthlyBudgetSnapshot>(snapshot.Id);
        }
        return snapshots.Count;
    }

    // Monthly Recurring Expense Snapshot methods
    public async Task<List<MonthlyRecurringExpenseSnapshot>> GetMonthlyRecurringExpenseSnapshotsAsync(int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<MonthlyRecurringExpenseSnapshot>()
            .Where(e => e.Month == month && e.Year == year)
            .ToListAsync();
    }

    public async Task<MonthlyRecurringExpenseSnapshot> InsertMonthlyRecurringExpenseSnapshotAsync(MonthlyRecurringExpenseSnapshot snapshot)
    {
        await InitializeAsync();
        await _database.InsertAsync(snapshot);
        return snapshot;
    }

    public async Task<int> DeleteMonthlyRecurringExpenseSnapshotsAsync(int month, int year)
    {
        await InitializeAsync();
        var snapshots = await GetMonthlyRecurringExpenseSnapshotsAsync(month, year);
        foreach (var snapshot in snapshots)
        {
            await _database.DeleteAsync<MonthlyRecurringExpenseSnapshot>(snapshot.Id);
        }
        return snapshots.Count;
    }

    // Monthly Income Snapshot methods
    public async Task<List<MonthlyIncomeSnapshot>> GetMonthlyIncomeSnapshotsAsync(int month, int year)
    {
        await InitializeAsync();
        return await _database.Table<MonthlyIncomeSnapshot>()
            .Where(i => i.Month == month && i.Year == year)
            .ToListAsync();
    }

    public async Task<MonthlyIncomeSnapshot> InsertMonthlyIncomeSnapshotAsync(MonthlyIncomeSnapshot snapshot)
    {
        await InitializeAsync();
        await _database.InsertAsync(snapshot);
        return snapshot;
    }

    public async Task<int> DeleteMonthlyIncomeSnapshotsAsync(int month, int year)
    {
        await InitializeAsync();
        var snapshots = await GetMonthlyIncomeSnapshotsAsync(month, year);
        foreach (var snapshot in snapshots)
        {
            await _database.DeleteAsync<MonthlyIncomeSnapshot>(snapshot.Id);
        }
        return snapshots.Count;
    }

    // Clear entire database
    public async Task ClearAllDataAsync()
    {
        await InitializeAsync();

        // Delete all data from all tables
        await _database.DeleteAllAsync<Expense>();
        await _database.DeleteAllAsync<PaymentRecord>();
        await _database.DeleteAllAsync<IncomeRecord>();
        await _database.DeleteAllAsync<BudgetSpending>();
        await _database.DeleteAllAsync<Budget>();
        await _database.DeleteAllAsync<RecurringExpense>();
        await _database.DeleteAllAsync<Income>();
        await _database.DeleteAllAsync<BankBalance>();
        await _database.DeleteAllAsync<Category>();

        // Delete all snapshot data
        await _database.DeleteAllAsync<MonthStatus>();
        await _database.DeleteAllAsync<MonthlyBudgetSnapshot>();
        await _database.DeleteAllAsync<MonthlyRecurringExpenseSnapshot>();
        await _database.DeleteAllAsync<MonthlyIncomeSnapshot>();

        Console.WriteLine("All database tables cleared successfully");
    }
}
