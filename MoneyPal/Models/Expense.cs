using SQLite;

namespace MoneyPal.Models;

[Table("Expenses")]
public class Expense
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Indexed]
    public Guid? BudgetId { get; set; } // Nullable: null for one-time expenses, set for budget expenses

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}
