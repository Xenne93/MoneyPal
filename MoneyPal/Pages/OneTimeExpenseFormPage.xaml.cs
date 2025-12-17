using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class OneTimeExpenseFormPage : ContentPage
{
    private readonly ITransactionService _transactionService;
    private readonly ILocalizationService _localizationService;
    private Expense? _editingExpense;

    public OneTimeExpenseFormPage(ITransactionService transactionService, ILocalizationService localizationService, int month, int year)
    {
        InitializeComponent();
        _transactionService = transactionService;
        _localizationService = localizationService;

        // Set default date to the current viewing month
        DatePickerControl.Date = new DateTime(year, month, DateTime.Now.Day > DateTime.DaysInMonth(year, month) ? DateTime.DaysInMonth(year, month) : DateTime.Now.Day);
    }

    public OneTimeExpenseFormPage(ITransactionService transactionService, ILocalizationService localizationService, Guid expenseId)
    {
        InitializeComponent();
        _transactionService = transactionService;
        _localizationService = localizationService;

        // Load expense for editing
        LoadExpenseAsync(expenseId);
    }

    private async void LoadExpenseAsync(Guid expenseId)
    {
        try
        {
            _editingExpense = await _transactionService.GetExpenseByIdAsync(expenseId);
            if (_editingExpense != null)
            {
                NameEntry.Text = _editingExpense.Name;
                AmountEntry.Text = _editingExpense.Amount.ToString("F2");
                DatePickerControl.Date = _editingExpense.Date;
                DescriptionEditor.Text = _editingExpense.Description;

                Title = "Uitgave Bewerken";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fout", $"Kon uitgave niet laden: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(NameEntry.Text))
            {
                await DisplayAlert("Fout", "Vul een naam in", "OK");
                return;
            }

            // Validate amount
            if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
            {
                await DisplayAlert("Fout", "Vul een geldig bedrag in", "OK");
                return;
            }

            // Create or update expense
            var expense = _editingExpense ?? new Expense { Id = Guid.NewGuid() };
            expense.Name = NameEntry.Text.Trim();
            expense.Amount = amount;
            expense.Date = DatePickerControl.Date;
            expense.Description = DescriptionEditor.Text?.Trim();
            expense.BudgetId = null; // Key: null means this is a one-time expense

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
        catch (Exception ex)
        {
            await DisplayAlert("Fout", $"Kon uitgave niet opslaan: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
