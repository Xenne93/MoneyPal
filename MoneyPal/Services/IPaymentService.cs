using MoneyPal.Models;

namespace MoneyPal.Services;

public interface IPaymentService
{
    Task<bool> MarkAsPaidAsync(Guid expenseId, int month, int year);
    Task<bool> MarkAsUnpaidAsync(Guid expenseId, int month, int year);
    Task<bool> IsExpensePaidAsync(Guid expenseId, int month, int year);
    Task<List<PaymentRecord>> GetPaymentRecordsForMonthAsync(int month, int year);
    Task<decimal> GetTotalUnpaidExpensesAsync(int month, int year);
    Task<int> GetUnpaidCountAsync(int month, int year);

    // Income tracking methods
    Task<bool> MarkIncomeAsReceivedAsync(Guid incomeId, int month, int year);
    Task<bool> MarkIncomeAsNotReceivedAsync(Guid incomeId, int month, int year);
    Task<bool> IsIncomeReceivedAsync(Guid incomeId, int month, int year);
}
