using MoneyPal.Pages;
using MoneyPal.Services;

namespace MoneyPal;

public partial class AppShell : Shell
{
    private readonly ILocalizationService _localization;

    public AppShell(ILocalizationService localization)
    {
        InitializeComponent();
        _localization = localization;

        // Register routes for pages not in TabBar
        Routing.RegisterRoute("budgets", typeof(BudgetsPage));
        Routing.RegisterRoute("expenses", typeof(RecurringExpensesPage));
        Routing.RegisterRoute("incomes", typeof(IncomesPage));

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        // Set initial tab titles
        UpdateTabTitles();
    }

    private void UpdateTabTitles()
    {
        // Update all tab titles with localized text
        HomeTab.Title = _localization.GetString("Navigation.Home");
        MonthlyTab.Title = _localization.GetString("MonthlyOverview.Title");
        SettingsTab.Title = _localization.GetString("Settings.Title");
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateTabTitles();
    }
}
