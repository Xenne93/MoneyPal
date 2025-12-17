using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class IncomeFormPage : ContentPage
{
    private readonly IIncomeService _incomeService;
    private readonly ILocalizationService _localization;
    private readonly Income? _editingIncome;

    public IncomeFormPage(IIncomeService incomeService, ILocalizationService localization, Income? editingIncome = null)
    {
        InitializeComponent();
        _incomeService = incomeService;
        _localization = localization;
        _editingIncome = editingIncome;

        // Set default category
        CategoryPicker.SelectedIndex = 0;

        if (_editingIncome != null)
        {
            PageTitle.Text = "Inkomen Bewerken";
            NameEntry.Text = _editingIncome.Name;
            AmountEntry.Text = _editingIncome.Amount.ToString("F2");
            DayEntry.Text = _editingIncome.DayOfMonth.ToString();
            DescriptionEditor.Text = _editingIncome.Description;

            // Set category
            var categoryIndex = CategoryPicker.Items.IndexOf(_editingIncome.Category);
            if (categoryIndex >= 0)
                CategoryPicker.SelectedIndex = categoryIndex;
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

        if (!int.TryParse(DayEntry.Text, out int day) || day < 1 || day > 31)
        {
            await DisplayAlert("Fout", "Vul een geldige dag in (1-31)", "OK");
            return;
        }

        if (CategoryPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Fout", "Selecteer een categorie", "OK");
            return;
        }

        // Create or update income
        var income = _editingIncome ?? new Income { Id = Guid.NewGuid() };
        income.Name = NameEntry.Text.Trim();
        income.Amount = amount;
        income.DayOfMonth = day;
        income.Category = CategoryPicker.SelectedItem.ToString() ?? "Anders";
        income.Description = DescriptionEditor.Text?.Trim();
        income.IsActive = true;

        if (_editingIncome == null)
        {
            await _incomeService.AddIncomeAsync(income);
        }
        else
        {
            await _incomeService.UpdateIncomeAsync(income);
        }

        await Navigation.PopModalAsync();
    }
}
