using MoneyPal.Models;

namespace MoneyPal.Services;

public interface IBankBalanceService
{
    Task<BankBalance> GetBankBalanceAsync(int month, int year);
    Task<BankBalance> UpdateBankBalanceAsync(int month, int year, decimal newBalance);
}
