using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class RecurringExpensesPage : ContentPage
{
    private readonly IExpenseService _expenseService;
    private readonly ILocalizationService _localization;
    private List<RecurringExpense> _expenses = new();
    private List<Category> _categories = new();

    public RecurringExpensesPage(IExpenseService expenseService, ILocalizationService localization)
    {
        InitializeComponent();
        _expenseService = expenseService;
        _localization = localization;

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        // Update localized texts
        UpdateLocalizedTexts();
    }

    private void UpdateLocalizedTexts()
    {
        Title = _localization.GetString("Expenses.Title");
        NoExpensesLabel.Text = _localization.GetString("Expenses.NoExpenses");
        AddFirstExpenseLabel.Text = _localization.GetString("Expenses.AddFirstExpense");
        AddExpenseButton.Text = _localization.GetString("Expenses.AddButton");
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLocalizedTexts();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async Task LoadData()
    {
        _expenses = await _expenseService.GetAllExpensesAsync();
        _categories = await _expenseService.GetAllCategoriesAsync();

        var totalMonthly = await _expenseService.GetTotalMonthlyExpensesAsync();

        // Update UI
        if (_expenses.Any())
        {
            SubtitleLabel.Text = $"{_localization.GetString("Expenses.Total")}: {_localization.GetString("Common.Currency")}{totalMonthly:N2} {_localization.GetString("Expenses.PerMonth")}";
            SubtitleLabel.IsVisible = true;
            EmptyState.IsVisible = false;
            ExpensesCollection.IsVisible = true;
            ExpensesCollection.ItemsSource = _expenses;
        }
        else
        {
            SubtitleLabel.IsVisible = false;
            EmptyState.IsVisible = true;
            ExpensesCollection.IsVisible = false;
        }
    }

    private async void OnAddExpenseClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new ExpenseFormPage(_expenseService, _localization, _categories));
    }

    private async void OnEditExpenseClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is RecurringExpense expense)
        {
            await Navigation.PushModalAsync(new ExpenseFormPage(_expenseService, _localization, _categories, expense));
        }
    }

    private async void OnDeleteExpenseClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is RecurringExpense expense)
        {
            bool confirm = await DisplayAlert(
                _localization.GetString("Common.Confirm"),
                $"{_localization.GetString("Expenses.DeleteConfirm")} '{expense.Name}'?",
                _localization.GetString("Common.Yes"),
                _localization.GetString("Common.No"));

            if (confirm)
            {
                await _expenseService.DeleteExpenseAsync(expense.Id);
                await LoadData();
            }
        }
    }
}
