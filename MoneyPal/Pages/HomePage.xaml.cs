using System.Globalization;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class HomePage : ContentPage
{
    private readonly ILocalizationService _localization;

    public HomePage(ILocalizationService localization)
    {
        InitializeComponent();
        _localization = localization;

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        // Update localized texts
        UpdateLocalizedTexts();
        UpdateDateTime();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateDateTime();
    }

    private void UpdateLocalizedTexts()
    {
        // Update page title
        Title = _localization.GetString("Dashboard.Title");

        // Update card labels
        BankAccountLabel.Text = _localization.GetString("Dashboard.BankAccount");
        CurrentBalanceLabel.Text = _localization.GetString("Dashboard.CurrentBalance");
        SavingsAccountLabel.Text = _localization.GetString("Dashboard.SavingsAccount");
        SavedAmountLabel.Text = _localization.GetString("Dashboard.SavedAmount");
        AvailableLabel.Text = _localization.GetString("Dashboard.Available");
        UntilNextSalaryLabel.Text = _localization.GetString("Dashboard.UntilNextSalary");
        UpcomingExpensesLabel.Text = _localization.GetString("Dashboard.UpcomingExpenses");
        ThisMonthLabel.Text = _localization.GetString("Dashboard.ThisMonth");

        // Update section headers
        RecentTransactionsLabel.Text = _localization.GetString("Dashboard.RecentTransactions");
        ViewAllLabel.Text = _localization.GetString("Dashboard.ViewAll");
        BudgetOverviewLabel.Text = _localization.GetString("Dashboard.BudgetOverview");
        ManageLabel.Text = _localization.GetString("Dashboard.Manage");

        // Update empty states
        NoTransactionsLabel.Text = _localization.GetString("Dashboard.NoTransactions");
        AddFirstTransactionLabel.Text = _localization.GetString("Dashboard.AddFirstTransaction");
        NoBudgetsLabel.Text = _localization.GetString("Dashboard.NoBudgets");
        CreateFirstBudgetLabel.Text = _localization.GetString("Dashboard.CreateFirstBudget");
    }

    private void UpdateDateTime()
    {
        var culture = new CultureInfo(_localization.CurrentLanguage);
        DateLabel.Text = DateTime.Now.ToString("dddd, d MMMM yyyy", culture);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLocalizedTexts();
        UpdateDateTime();
    }

    private async void OnManageBudgetsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//budgets");
    }
}
