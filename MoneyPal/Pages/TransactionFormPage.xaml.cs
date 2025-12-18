using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class TransactionFormPage : ContentPage
{
    private readonly ITransactionService _transactionService;
    private readonly IBudgetService _budgetService;
    private readonly Budget _budget;
    private readonly Expense? _editingExpense;
    private readonly int _month;
    private readonly int _year;

    public TransactionFormPage(ITransactionService transactionService, IBudgetService budgetService, Budget budget, int month, int year, Expense? editingExpense = null)
    {
        InitializeComponent();
        _transactionService = transactionService;
        _budgetService = budgetService;
        _budget = budget;
        _month = month;
        _year = year;
        _editingExpense = editingExpense;

        BudgetLabel.Text = $"💰 {_budget.Name}";

        if (_editingExpense != null)
        {
            PageTitle.Text = "Uitgave Bewerken";
            NameEntry.Text = _editingExpense.Name;
            AmountEntry.Text = _editingExpense.Amount.ToString("F2");
            DatePicker.Date = _editingExpense.Date;
            DescriptionEditor.Text = _editingExpense.Description;
        }
        else
        {
            // Set date to the first day of the specified month/year
            DatePicker.Date = new DateTime(_year, _month, 1);
        }

        // Restrict date picker to the specified month
        DatePicker.MinimumDate = new DateTime(_year, _month, 1);
        DatePicker.MaximumDate = new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month));
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlert("Fout", "Vul een naam in", "OK");
            return;
        }

        if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
        {
            await DisplayAlert("Fout", "Vul een geldig bedrag in", "OK");
            return;
        }

        // Create or update expense
        var expense = _editingExpense ?? new Expense { Id = Guid.NewGuid() };
        expense.BudgetId = _budget.Id;
        expense.Name = NameEntry.Text.Trim();
        expense.Amount = amount;
        expense.Date = DatePicker.Date;
        expense.Description = DescriptionEditor.Text?.Trim();

        if (_editingExpense == null)
        {
            await _transactionService.AddExpenseAsync(expense);
        }
        else
        {
            await _transactionService.UpdateExpenseAsync(expense);
        }

        await Navigation.PopModalAsync();
    }
}
