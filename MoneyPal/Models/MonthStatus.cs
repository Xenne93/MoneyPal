using SQLite;

namespace MoneyPal.Models
{
    public class MonthStatus
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public int Month { get; set; } // 1-12

        [Indexed]
        public int Year { get; set; }

        public bool IsInitialized { get; set; }

        public DateTime InitializedAt { get; set; }

        public DateTime? LastRegeneratedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public MonthStatus()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
