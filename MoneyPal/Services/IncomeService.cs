using MoneyPal.Models;

namespace MoneyPal.Services;

public class IncomeService : IIncomeService
{
    private readonly DataStorageService _storage;

    public IncomeService(IDataStorageService storage)
    {
        _storage = (DataStorageService)storage;
    }

    public async Task<Income> AddIncomeAsync(Income income)
    {
        return await _storage.InsertIncomeAsync(income);
    }

    public async Task<Income> UpdateIncomeAsync(Income income)
    {
        await _storage.UpdateIncomeAsync(income);
        return income;
    }

    public async Task<bool> DeleteIncomeAsync(Guid incomeId)
    {
        var result = await _storage.DeleteIncomeAsync(incomeId);
        return result > 0;
    }

    public async Task<Income?> GetIncomeByIdAsync(Guid incomeId)
    {
        return await _storage.GetIncomeByIdAsync(incomeId);
    }

    public async Task<List<Income>> GetAllIncomesAsync()
    {
        return await _storage.GetAllIncomesAsync();
    }

    public async Task<decimal> GetTotalMonthlyIncomeAsync()
    {
        return await _storage.GetTotalMonthlyIncomesAsync();
    }
}
