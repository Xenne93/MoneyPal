using SQLite;
using System.ComponentModel.DataAnnotations;

namespace MoneyPal.Models;

[Table("BudgetSpending")]
public class BudgetSpending
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Indexed]
    public Guid BudgetId { get; set; }

    public int Month { get; set; } // 1-12

    public int Year { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AmountSpent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }
}
