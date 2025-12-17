using System.ComponentModel.DataAnnotations;
using SQLite;

namespace MoneyPal.Models;

[Table("RecurringExpenses")]
public class RecurringExpense
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name must be less than 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Day of month is required")]
    [Range(1, 31, ErrorMessage = "Day must be between 1 and 31")]
    public int DayOfMonth { get; set; }

    [Required(ErrorMessage = "Category is required")]
    [Indexed]
    public Guid CategoryId { get; set; }

    [StringLength(500, ErrorMessage = "Description must be less than 500 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }
}
