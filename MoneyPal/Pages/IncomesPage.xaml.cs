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
                "Verwijderen",
                $"Weet je zeker dat je '{income.Name}' wilt verwijderen?",
                "Ja",
                "Nee");

            if (confirm)
            {
                await _incomeService.DeleteIncomeAsync(income.Id);
                await LoadData();
            }
        }
    }
}
