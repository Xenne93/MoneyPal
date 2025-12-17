using MoneyPal.Models;

namespace MoneyPal.Services;

public class BankBalanceService : IBankBalanceService
{
    private readonly DataStorageService _storage;

    public BankBalanceService(IDataStorageService storage)
    {
        _storage = (DataStorageService)storage;
    }

    public async Task<BankBalance> GetBankBalanceAsync(int month, int year)
    {
        return await _storage.GetBankBalanceAsync(month, year);
    }

    public async Task<BankBalance> UpdateBankBalanceAsync(int month, int year, decimal newBalance)
    {
        return await _storage.UpdateBankBalanceAsync(month, year, newBalance);
    }
}
