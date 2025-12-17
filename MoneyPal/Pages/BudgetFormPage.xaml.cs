using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class BudgetFormPage : ContentPage
{
    private readonly IBudgetService _budgetService;
    private readonly ILocalizationService _localization;
    private readonly Budget? _editingBudget;

    public BudgetFormPage(IBudgetService budgetService, ILocalizationService localization, Budget? editingBudget = null)
    {
        InitializeComponent();
        _budgetService = budgetService;
        _localization = localization;
        _editingBudget = editingBudget;

        if (_editingBudget != null)
        {
            PageTitle.Text = "Budget bewerken";
            NameEntry.Text = _editingBudget.Name;
            AmountEntry.Text = _editingBudget.Amount.ToString("F2");
            DescriptionEditor.Text = _editingBudget.Description;
            FixedExpenseCheckbox.IsChecked = _editingBudget.CountAsFixedExpense;
        }
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

        // Create or update budget
        var budget = _editingBudget ?? new Budget { Id = Guid.NewGuid() };
        budget.Name = NameEntry.Text.Trim();
        budget.Amount = amount;
        budget.Description = DescriptionEditor.Text?.Trim();
        budget.CountAsFixedExpense = FixedExpenseCheckbox.IsChecked;
        budget.IsActive = true;

        if (_editingBudget == null)
        {
            await _budgetService.AddBudgetAsync(budget);
        }
        else
        {
            await _budgetService.UpdateBudgetAsync(budget);
        }

        await Navigation.PopModalAsync();
    }
}
