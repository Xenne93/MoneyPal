using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class ExpenseFormPage : ContentPage
{
    private readonly IExpenseService _expenseService;
    private readonly ILocalizationService _localization;
    private readonly List<Category> _categories;
    private readonly RecurringExpense? _editingExpense;

    public ExpenseFormPage(IExpenseService expenseService, ILocalizationService localization, List<Category> categories, RecurringExpense? editingExpense = null)
    {
        InitializeComponent();
        _expenseService = expenseService;
        _localization = localization;
        _categories = categories;
        _editingExpense = editingExpense;

        CategoryPicker.ItemsSource = categories.Select(c => $"{c.Icon} {c.Name}").ToList();

        if (_editingExpense != null)
        {
            PageTitle.Text = "Uitgave bewerken";
            NameEntry.Text = _editingExpense.Name;
            AmountEntry.Text = _editingExpense.Amount.ToString();
            DayEntry.Text = _editingExpense.DayOfMonth.ToString();
            DescriptionEditor.Text = _editingExpense.Description;

            var categoryIndex = categories.FindIndex(c => c.Id == _editingExpense.CategoryId);
            if (categoryIndex >= 0)
                CategoryPicker.SelectedIndex = categoryIndex;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
            !decimal.TryParse(AmountEntry.Text, out decimal amount) ||
            !int.TryParse(DayEntry.Text, out int day) ||
            CategoryPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Fout", "Vul alle verplichte velden in", "OK");
            return;
        }

        var expense = _editingExpense ?? new RecurringExpense { Id = Guid.NewGuid() };
        expense.Name = NameEntry.Text;
        expense.Amount = amount;
        expense.DayOfMonth = day;
        expense.CategoryId = _categories[CategoryPicker.SelectedIndex].Id;
        expense.Description = DescriptionEditor.Text;
        expense.IsActive = true;

        if (_editingExpense == null)
            await _expenseService.AddExpenseAsync(expense);
        else
            await _expenseService.UpdateExpenseAsync(expense);

        await Navigation.PopModalAsync();
    }
}
