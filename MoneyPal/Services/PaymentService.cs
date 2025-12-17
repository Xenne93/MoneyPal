using MoneyPal.Models;

namespace MoneyPal.Services;

public class PaymentService : IPaymentService
{
    private readonly IDataStorageService _storage;
    private readonly IExpenseService _expenseService;

    public PaymentService(IDataStorageService storage, IExpenseService expenseService)
    {
        _storage = storage;
        _expenseService = expenseService;
    }

    public async Task<bool> MarkAsPaidAsync(Guid expenseId, int month, int year)
    {
        var record = new PaymentRecord
        {
            ExpenseId = expenseId,
            Month = month,
            Year = year,
            IsPaid = true,
            PaidDate = DateTime.UtcNow
        };

        await ((DataStorageService)_storage).UpsertPaymentRecordAsync(record);
        return true;
    }

    public async Task<bool> MarkAsUnpaidAsync(Guid expenseId, int month, int year)
    {
        var record = new PaymentRecord
        {
            ExpenseId = expenseId,
            Month = month,
            Year = year,
            IsPaid = false,
            PaidDate = null
        };

        await ((DataStorageService)_storage).UpsertPaymentRecordAsync(record);
        return true;
    }

    public async Task<bool> IsExpensePaidAsync(Guid expenseId, int month, int year)
    {
        var record = await ((DataStorageService)_storage).GetPaymentRecordAsync(expenseId, month, year);
        return record?.IsPaid ?? false;
    }

    public async Task<List<PaymentRecord>> GetPaymentRecordsForMonthAsync(int month, int year)
    {
        return await ((DataStorageService)_storage).GetPaymentRecordsForMonthAsync(month, year);
    }

    public async Task<decimal> GetTotalUnpaidExpensesAsync(int month, int year)
    {
        var allExpenses = await _expenseService.GetAllExpensesAsync();
        var paymentRecords = await GetPaymentRecordsForMonthAsync(month, year);

        decimal total = 0;
        foreach (var expense in allExpenses.Where(e => e.IsActive))
        {
            var isPaid = paymentRecords.Any(p => p.ExpenseId == expense.Id && p.IsPaid);
            if (!isPaid)
            {
                total += expense.Amount;
            }
        }

        return total;
    }

    public async Task<int> GetUnpaidCountAsync(int month, int year)
    {
        var allExpenses = await _expenseService.GetAllExpensesAsync();
        var paymentRecords = await GetPaymentRecordsForMonthAsync(month, year);

        int count = 0;
        foreach (var expense in allExpenses.Where(e => e.IsActive))
        {
            var isPaid = paymentRecords.Any(p => p.ExpenseId == expense.Id && p.IsPaid);
            if (!isPaid)
            {
                count++;
            }
        }

        return count;
    }
}
