using SQLite;

namespace MoneyPal.Models;

[Table("BankBalance")]
public class BankBalance
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Indexed]
    public int Month { get; set; }

    [Indexed]
    public int Year { get; set; }

    public decimal CurrentBalance { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
