using SQLite;

namespace MoneyPal.Models;

[Table("PaymentRecords")]
public class PaymentRecord
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Indexed]
    public Guid ExpenseId { get; set; }

    public int Month { get; set; } // 1-12

    public int Year { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    // Helper to create a unique key for month/year/expense combination
    public static string GetKey(Guid expenseId, int month, int year)
    {
        return $"{expenseId}_{year}_{month:D2}";
    }
}
