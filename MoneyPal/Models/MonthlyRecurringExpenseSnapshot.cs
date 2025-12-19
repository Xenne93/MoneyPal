using SQLite;

namespace MoneyPal.Models
{
    /// <summary>
    /// Snapshot of a recurring expense for a specific month.
    /// Once a month is initialized, this snapshot preserves the expense as it was at that time.
    /// </summary>
    public class MonthlyRecurringExpenseSnapshot
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public int Month { get; set; } // 1-12

        [Indexed]
        public int Year { get; set; }

        // Reference to original recurring expense (for tracking purposes)
        [Indexed]
        public Guid OriginalExpenseId { get; set; }

        // Snapshot data (copied from RecurringExpense at time of month initialization)
        public string Name { get; set; }

        public decimal Amount { get; set; }

        public int DayOfMonth { get; set; }

        public Guid? CategoryId { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ModifiedAt { get; set; }

        public MonthlyRecurringExpenseSnapshot()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }
    }
}
