using SQLite;

namespace MoneyPal.Models;

[Table("IncomeRecords")]
public class IncomeRecord
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Indexed]
    public Guid IncomeId { get; set; }

    public int Month { get; set; } // 1-12

    public int Year { get; set; }

    public bool IsReceived { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    // Helper to create a unique key for month/year/income combination
    public static string GetKey(Guid incomeId, int month, int year)
    {
        return $"{incomeId}_{year}_{month:D2}";
    }
}
