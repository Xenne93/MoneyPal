using System.Globalization;
using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class BudgetDetailPage : ContentPage
{
    private readonly ITransactionService _transactionService;
    private readonly IBudgetService _budgetService;
    private readonly IBankBalanceService _bankBalanceService;
    private readonly ILocalizationService _localization;
    private readonly Budget _budget;
    private readonly int _month;
    private readonly int _year;
    private List<Expense> _expenses = new();

    public BudgetDetailPage(ITransactionService transactionService, IBudgetService budgetService,
        IBankBalanceService bankBalanceService, ILocalizationService localization,
        Budget budget, int month, int year)
    {
        InitializeComponent();
        _transactionService = transactionService;
        _budgetService = budgetService;
        _bankBalanceService = bankBalanceService;
        _localization = localization;
        _budget = budget;
        _month = month;
        _year = year;

        BudgetNameLabel.Text = _budget.Name;

        var date = new DateTime(_year, _month, 1);
        MonthLabel.Text = date.ToString("MMMM yyyy", new CultureInfo("nl-NL"));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async Task LoadData()
    {
        _expenses = await _transactionService.GetExpensesForBudgetAsync(_budget.Id, _month, _year);

        var totalSpent = _expenses.Sum(e => e.Amount);
        var remaining = _budget.Amount - totalSpent;

        BudgetLimitLabel.Text = $"€ {_budget.Amount:N2}";
        SpentLabel.Text = $"€ {totalSpent:N2}";
        RemainingLabel.Text = $"€ {remaining:N2}";

        if (_expenses.Any())
        {
            EmptyState.IsVisible = false;
            ExpensesCollection.IsVisible = true;
            ExpensesCollection.ItemsSource = _expenses;
        }
        else
        {
            EmptyState.IsVisible = true;
            ExpensesCollection.IsVisible = false;
        }
    }

    private async void OnAddExpenseClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new TransactionFormPage(_transactionService, _budgetService, _bankBalanceService, _localization, _budget, _month, _year));
        // Reload data when modal is closed
        await LoadData();
    }

    private async void OnEditExpenseClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Expense expense)
        {
            await Navigation.PushModalAsync(new TransactionFormPage(_transactionService, _budgetService, _bankBalanceService, _localization, _budget, _month, _year, expense));
            // Reload data when modal is closed
            await LoadData();
        }
    }

    private async void OnDeleteExpenseClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Expense expense)
        {
            bool confirm = await DisplayAlert(
                "Verwijderen",
                $"Weet je zeker dat je '{expense.Name}' wilt verwijderen?",
                "Ja",
                "Nee");

            if (confirm)
            {
                await _transactionService.DeleteExpenseAsync(expense.Id);
                await LoadData();
            }
        }
    }
}
