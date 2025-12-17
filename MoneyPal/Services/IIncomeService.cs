using MoneyPal.Models;

namespace MoneyPal.Services;

public interface IIncomeService
{
    Task<Income> AddIncomeAsync(Income income);
    Task<Income> UpdateIncomeAsync(Income income);
    Task<bool> DeleteIncomeAsync(Guid incomeId);
    Task<Income?> GetIncomeByIdAsync(Guid incomeId);
    Task<List<Income>> GetAllIncomesAsync();
    Task<decimal> GetTotalMonthlyIncomeAsync();
}
