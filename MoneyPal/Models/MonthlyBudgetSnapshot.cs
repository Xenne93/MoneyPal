using SQLite;

namespace MoneyPal.Models
{
    /// <summary>
    /// Snapshot of a budget for a specific month.
    /// Once a month is initialized, this snapshot preserves the budget as it was at that time.
    /// </summary>
    public class MonthlyBudgetSnapshot
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public int Month { get; set; } // 1-12

        [Indexed]
        public int Year { get; set; }

        // Reference to original budget (for tracking purposes)
        [Indexed]
        public Guid OriginalBudgetId { get; set; }

        // Snapshot data (copied from Budget at time of month initialization)
        public string Name { get; set; }

        public decimal Amount { get; set; }

        public Guid? CategoryId { get; set; }

        public string Description { get; set; }

        public bool CountAsFixedExpense { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ModifiedAt { get; set; }

        public MonthlyBudgetSnapshot()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }
    }
}
