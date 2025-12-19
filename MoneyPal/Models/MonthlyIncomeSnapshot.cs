using SQLite;

namespace MoneyPal.Models
{
    /// <summary>
    /// Snapshot of an income source for a specific month.
    /// Once a month is initialized, this snapshot preserves the income as it was at that time.
    /// </summary>
    public class MonthlyIncomeSnapshot
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public int Month { get; set; } // 1-12

        [Indexed]
        public int Year { get; set; }

        // Reference to original income (for tracking purposes)
        [Indexed]
        public Guid OriginalIncomeId { get; set; }

        // Snapshot data (copied from Income at time of month initialization)
        public string Name { get; set; }

        public decimal Amount { get; set; }

        public int DayOfMonth { get; set; }

        public string Category { get; set; } // "Salaris", "Subsidie", "Terugave", "Anders"

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ModifiedAt { get; set; }

        public MonthlyIncomeSnapshot()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }
    }
}
