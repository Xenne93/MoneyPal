using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class BudgetsPage : ContentPage
{
    private readonly IBudgetService _budgetService;
    private readonly ILocalizationService _localization;
    private List<Budget> _budgets = new();

    public BudgetsPage(IBudgetService budgetService, ILocalizationService localization)
    {
        InitializeComponent();
        _budgetService = budgetService;
        _localization = localization;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async Task LoadData()
    {
        _budgets = await _budgetService.GetAllBudgetsAsync();

        if (_budgets.Any())
        {
            var total = _budgets.Where(b => b.IsActive).Sum(b => b.Amount);
            var fixedTotal = _budgets.Where(b => b.IsActive && b.CountAsFixedExpense).Sum(b => b.Amount);
            var flexibleTotal = _budgets.Where(b => b.IsActive && !b.CountAsFixedExpense).Sum(b => b.Amount);

            TotalBudgetLabel.Text = $"€ {total:N2}";
            FixedBudgetLabel.Text = $"€ {fixedTotal:N2}";
            FlexibleBudgetLabel.Text = $"€ {flexibleTotal:N2}";

            SummaryGrid.IsVisible = true;
            EmptyState.IsVisible = false;
            BudgetsCollection.IsVisible = true;
            BudgetsCollection.ItemsSource = _budgets.Where(b => b.IsActive).OrderBy(b => b.Name).ToList();
        }
        else
        {
            SummaryGrid.IsVisible = false;
            EmptyState.IsVisible = true;
            BudgetsCollection.IsVisible = false;
        }
    }

    private async void OnAddBudgetClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new BudgetFormPage(_budgetService, _localization));
    }

    private async void OnEditBudgetClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Budget budget)
        {
            await Navigation.PushModalAsync(new BudgetFormPage(_budgetService, _localization, budget));
        }
    }

    private async void OnDeleteBudgetClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Budget budget)
        {
            bool confirm = await DisplayAlert(
                "Verwijderen",
                $"Weet je zeker dat je '{budget.Name}' wilt verwijderen?",
                "Ja",
                "Nee");

            if (confirm)
            {
                await _budgetService.DeleteBudgetAsync(budget.Id);
                await LoadData();
            }
        }
    }
}
