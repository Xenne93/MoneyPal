using MoneyPal.Models;

namespace MoneyPal.Services
{
    public class MonthInitializationService
    {
        private readonly DataStorageService _dataStorage;

        public MonthInitializationService(DataStorageService dataStorage)
        {
            _dataStorage = dataStorage;
        }

        /// <summary>
        /// Checks if a month has been initialized (snapshots created).
        /// </summary>
        public async Task<bool> IsMonthInitializedAsync(int month, int year)
        {
            var status = await _dataStorage.GetMonthStatusAsync(month, year);
            return status?.IsInitialized ?? false;
        }

        /// <summary>
        /// Initializes a month by creating snapshots of current budgets, recurring expenses, and income.
        /// Also carries over bank balance from previous month.
        /// </summary>
        public async Task InitializeMonthAsync(int month, int year)
        {
            // Check if already initialized
            if (await IsMonthInitializedAsync(month, year))
            {
                throw new InvalidOperationException($"Month {month}/{year} is already initialized.");
            }

            // 1. Create snapshots of budgets
            var budgets = await _dataStorage.GetAllBudgetsAsync();
            var activeBudgets = budgets.Where(b => b.IsActive).ToList();

            foreach (var budget in activeBudgets)
            {
                var snapshot = new MonthlyBudgetSnapshot
                {
                    Month = month,
                    Year = year,
                    OriginalBudgetId = budget.Id,
                    Name = budget.Name,
                    Amount = budget.Amount,
                    CategoryId = budget.CategoryId,
                    Description = budget.Description,
                    CountAsFixedExpense = budget.CountAsFixedExpense
                };

                await _dataStorage.InsertMonthlyBudgetSnapshotAsync(snapshot);
            }

            // 2. Create snapshots of recurring expenses
            var expenses = await _dataStorage.GetAllExpensesAsync();
            var activeExpenses = expenses.Where(e => e.IsActive).ToList();

            foreach (var expense in activeExpenses)
            {
                var snapshot = new MonthlyRecurringExpenseSnapshot
                {
                    Month = month,
                    Year = year,
                    OriginalExpenseId = expense.Id,
                    Name = expense.Name,
                    Amount = expense.Amount,
                    DayOfMonth = expense.DayOfMonth,
                    CategoryId = expense.CategoryId,
                    Description = expense.Description
                };

                await _dataStorage.InsertMonthlyRecurringExpenseSnapshotAsync(snapshot);

                // Create payment record for this expense (initially unpaid)
                var paymentRecord = new PaymentRecord
                {
                    ExpenseId = expense.Id,
                    Month = month,
                    Year = year,
                    IsPaid = false,
                    PaidDate = null
                };

                await _dataStorage.UpsertPaymentRecordAsync(paymentRecord);
            }

            // 3. Create snapshots of income
            var incomes = await _dataStorage.GetAllIncomesAsync();
            var activeIncomes = incomes.Where(i => i.IsActive).ToList();

            foreach (var income in activeIncomes)
            {
                var snapshot = new MonthlyIncomeSnapshot
                {
                    Month = month,
                    Year = year,
                    OriginalIncomeId = income.Id,
                    Name = income.Name,
                    Amount = income.Amount,
                    DayOfMonth = income.DayOfMonth,
                    Category = income.Category,
                    Description = income.Description
                };

                await _dataStorage.InsertMonthlyIncomeSnapshotAsync(snapshot);
            }

            // 4. Carry over bank balance from previous month
            var previousMonth = month == 1 ? 12 : month - 1;
            var previousYear = month == 1 ? year - 1 : year;

            try
            {
                var previousBalance = await _dataStorage.GetBankBalanceAsync(previousMonth, previousYear);
                await _dataStorage.UpdateBankBalanceAsync(month, year, previousBalance.CurrentBalance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not carry over bank balance: {ex.Message}");
                // If previous month doesn't exist, start with 0 (which is the default)
            }

            // 5. Mark month as initialized
            var monthStatus = new MonthStatus
            {
                Month = month,
                Year = year,
                IsInitialized = true,
                InitializedAt = DateTime.UtcNow
            };

            await _dataStorage.UpsertMonthStatusAsync(monthStatus);

            Console.WriteLine($"Month {month}/{year} initialized successfully");
        }

        /// <summary>
        /// Regenerates a month's snapshots. Can optionally preserve existing expenses and payment records.
        /// </summary>
        /// <param name="month">The month to regenerate (1-12)</param>
        /// <param name="year">The year</param>
        /// <param name="preserveUserData">If true, keeps existing Expense records and PaymentRecords. If false, resets everything.</param>
        public async Task RegenerateMonthAsync(int month, int year, bool preserveUserData = true)
        {
            Console.WriteLine($"Regenerating month {month}/{year} (preserveUserData: {preserveUserData})");

            // 1. Delete existing snapshots (but keep payment records and expenses if preserveUserData is true)
            await _dataStorage.DeleteMonthlyBudgetSnapshotsAsync(month, year);
            await _dataStorage.DeleteMonthlyRecurringExpenseSnapshotsAsync(month, year);
            await _dataStorage.DeleteMonthlyIncomeSnapshotsAsync(month, year);

            // 2. If not preserving user data, delete payment records
            // Note: We don't delete Expense records as they are user-created transactions
            // But we can reset payment records for recurring expenses
            if (!preserveUserData)
            {
                var paymentRecords = await _dataStorage.GetPaymentRecordsForMonthAsync(month, year);
                foreach (var record in paymentRecords)
                {
                    await _dataStorage.DeletePaymentRecordAsync(record.Id);
                }
            }

            // 3. Mark month as not initialized temporarily
            var monthStatus = await _dataStorage.GetMonthStatusAsync(month, year);
            if (monthStatus != null)
            {
                monthStatus.IsInitialized = false;
                await _dataStorage.UpsertMonthStatusAsync(monthStatus);
            }

            // 4. Re-initialize the month
            await InitializeMonthAsync(month, year);

            // 5. Update regeneration timestamp
            monthStatus = await _dataStorage.GetMonthStatusAsync(month, year);
            if (monthStatus != null)
            {
                monthStatus.LastRegeneratedAt = DateTime.UtcNow;
                await _dataStorage.UpsertMonthStatusAsync(monthStatus);
            }

            Console.WriteLine($"Month {month}/{year} regenerated successfully");
        }

        /// <summary>
        /// Gets the month status information
        /// </summary>
        public async Task<MonthStatus?> GetMonthStatusAsync(int month, int year)
        {
            return await _dataStorage.GetMonthStatusAsync(month, year);
        }
    }
}
