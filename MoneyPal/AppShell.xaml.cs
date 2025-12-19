using MoneyPal.Services;

namespace MoneyPal;

public partial class AppShell : Shell
{
    private readonly ILocalizationService _localization;

    public AppShell(ILocalizationService localization)
    {
        InitializeComponent();
        _localization = localization;

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        // Set initial tab titles
        UpdateTabTitles();
    }

    private void UpdateTabTitles()
    {
        // Update all tab titles with localized text
        HomeTab.Title = _localization.GetString("Navigation.Home");
        ExpensesTab.Title = _localization.GetString("Navigation.Expenses");
        IncomesTab.Title = _localization.GetString("Navigation.Income");
        MonthlyTab.Title = _localization.GetString("MonthlyOverview.Title");
        BudgetsTab.Title = _localization.GetString("Navigation.Budgets");
        SettingsTab.Title = _localization.GetString("Settings.Title");
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateTabTitles();
    }
}
