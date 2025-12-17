using System.Globalization;

namespace MoneyPal.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        DateLabel.Text = DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("nl-NL"));
    }

    private async void OnManageBudgetsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//budgets");
    }
}
