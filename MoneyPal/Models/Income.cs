using SQLite;

namespace MoneyPal.Models;

[Table("Incomes")]
public class Income
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public int DayOfMonth { get; set; } = 1;

    public string Category { get; set; } = "Salaris"; // Salaris, Subsidie, Terugave, Anders

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }
}
