using System.ComponentModel.DataAnnotations;
using SQLite;

namespace MoneyPal.Models;

[Table("Budgets")]
public class Budget
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name must be less than 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Indexed]
    public Guid? CategoryId { get; set; }

    public string? Description { get; set; }

    public bool CountAsFixedExpense { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }
}
