using System.ComponentModel.DataAnnotations;
using SQLite;

namespace MoneyPal.Models;

[Table("Categories")]
public class Category
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, ErrorMessage = "Name must be less than 50 characters")]
    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public string? Icon { get; set; }

    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static List<Category> GetDefaultCategories()
    {
        return new List<Category>
        {
            new Category { Name = "Housing", Icon = "🏠", Color = "#3b82f6", IsDefault = true },
            new Category { Name = "Utilities", Icon = "💡", Color = "#10b981", IsDefault = true },
            new Category { Name = "Insurance", Icon = "🛡️", Color = "#8b5cf6", IsDefault = true },
            new Category { Name = "Subscriptions", Icon = "📱", Color = "#f59e0b", IsDefault = true },
            new Category { Name = "Transportation", Icon = "🚗", Color = "#ef4444", IsDefault = true },
            new Category { Name = "Healthcare", Icon = "⚕️", Color = "#ec4899", IsDefault = true },
            new Category { Name = "Education", Icon = "📚", Color = "#6366f1", IsDefault = true },
            new Category { Name = "Other", Icon = "📦", Color = "#6b7280", IsDefault = true }
        };
    }
}
