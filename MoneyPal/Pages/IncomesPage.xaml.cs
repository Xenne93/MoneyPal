using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class IncomesPage : ContentPage
{
    private readonly IIncomeService _incomeService;
    private readonly ILocalizationService _localization;
    private List<Income> _incomes = new();

    public IncomesPage(IIncomeService incomeService, ILocalizationService localization)
    {
        InitializeComponent();
        _incomeService = incomeService;
        _localization = localization;

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        // Update localized texts
        UpdateLocalizedTexts();
    }

    private void UpdateLocalizedTexts()
    {
        Title = _localization.GetString("Income.Title");
        TotalMonthlyIncomeLabel.Text = _localization.GetString("Income.TotalMonthlyIncome");
        NoIncomesLabel.Text = _localization.GetString("Income.NoIncomes");
        AddFirstIncomeLabel.Text = _localization.GetString("Income.AddFirstIncome");
        AddIncomeButton.Text = _localization.GetString("Income.AddButton");
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
        _incomes = await _incomeService.GetAllIncomesAsync();

        if (_incomes.Any())
        {
            var total = _incomes.Where(i => i.IsActive).Sum(i => i.Amount);
            TotalIncomeLabel.Text = $"€ {total:N2}";

            EmptyState.IsVisible = false;
            IncomesCollection.IsVisible = true;
            IncomesCollection.ItemsSource = _incomes.Where(i => i.IsActive).OrderBy(i => i.DayOfMonth).ToList();
        }
        else
        {
            TotalIncomeLabel.Text = "€ 0,00";
            EmptyState.IsVisible = true;
            IncomesCollection.IsVisible = false;
        }
    }

    private async void OnAddIncomeClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new IncomeFormPage(_incomeService, _localization));
    }

    private async void OnEditIncomeClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Income income)
        {
            await Navigation.PushModalAsync(new IncomeFormPage(_incomeService, _localization, income));
        }
    }

    private async void OnDeleteIncomeClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Income income)
        {
            bool confirm = await DisplayAlert(
                _localization.GetString("Common.Delete"),
                string.Format(_localization.GetString("Income.DeleteConfirmMessage"), income.Name),
                _localization.GetString("Common.Yes"),
                _localization.GetString("Common.No"));

            if (confirm)
            {
                await _incomeService.DeleteIncomeAsync(income.Id);
                await LoadData();
            }
        }
    }
}
