using System.Globalization;
using System.Collections.ObjectModel;
using MoneyPal.Models;
using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class MonthlyOverviewPage : ContentPage
{
    private readonly IBudgetService _budgetService;
    private readonly ITransactionService _transactionService;
    private readonly IExpenseService _expenseService;
    private readonly IPaymentService _paymentService;
    private readonly IBankBalanceService _bankBalanceService;
    private readonly ILocalizationService _localization;

    private int _currentMonth;
    private int _currentYear;
    private ObservableCollection<MonthlyBudgetItem> _budgets = new();
    private ObservableCollection<MonthlyExpenseItem> _expenses = new();
    private ObservableCollection<OneTimeExpenseItem> _oneTimeExpenses = new();

    public MonthlyOverviewPage(IBudgetService budgetService, ITransactionService transactionService,
        IExpenseService expenseService, IPaymentService paymentService, IBankBalanceService bankBalanceService,
        ILocalizationService localization)
    {
        InitializeComponent();
        _budgetService = budgetService;
        _transactionService = transactionService;
        _expenseService = expenseService;
        _paymentService = paymentService;
        _bankBalanceService = bankBalanceService;
        _localization = localization;

        // Set to current month
        _currentMonth = DateTime.Now.Month;
        _currentYear = DateTime.Now.Year;

        BudgetsCollection.ItemsSource = _budgets;
        ExpensesCollection.ItemsSource = _expenses;
        OneTimeExpensesCollection.ItemsSource = _oneTimeExpenses;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async Task LoadData()
    {
        // Update month label
        var date = new DateTime(_currentYear, _currentMonth, 1);
        MonthLabel.Text = date.ToString("MMMM yyyy", new CultureInfo("nl-NL"));

        // Get all active budgets
        var budgets = await _budgetService.GetAllBudgetsAsync();
        var activeBudgets = budgets.Where(b => b.IsActive).ToList();

        // Get all active recurring expenses
        var expenses = await _expenseService.GetAllExpensesAsync();
        var activeExpenses = expenses.Where(e => e.IsActive).ToList();

        // Get one-time expenses for this month
        var oneTimeExpenses = await _transactionService.GetOneTimeExpensesForMonthAsync(_currentMonth, _currentYear);

        // Check if we have any data
        if (!activeBudgets.Any() && !activeExpenses.Any() && !oneTimeExpenses.Any())
        {
            EmptyState.IsVisible = true;
            BudgetsSection.IsVisible = false;
            RecurringExpensesSection.IsVisible = false;
            OneTimeExpensesSection.IsVisible = false;
            OneTimeExpensesCollection.IsVisible = false;
            TotalLabel.Text = "€ 0,00";
            PaidLabel.Text = "€ 0,00";
            UnpaidLabel.Text = "€ 0,00";
            return;
        }

        EmptyState.IsVisible = false;
        OneTimeExpensesSection.IsVisible = true; // Always show the section (with + button)

        decimal totalBudget = 0;
        decimal totalSpent = 0;
        decimal totalRecurringExpenses = 0;
        decimal totalRecurringPaid = 0;

        // Load budgets - create new list to avoid flickering
        var budgetItems = new List<MonthlyBudgetItem>();
        if (activeBudgets.Any())
        {
            BudgetsSection.IsVisible = true;

            foreach (var budget in activeBudgets.OrderBy(b => b.Name))
            {
                var spent = await _transactionService.GetTotalForBudgetAsync(budget.Id, _currentMonth, _currentYear);

                var item = new MonthlyBudgetItem
                {
                    Budget = budget,
                    BudgetId = budget.Id,
                    Name = budget.Name,
                    BudgetAmount = budget.Amount,
                    SpentAmount = spent,
                    RemainingAmount = budget.Amount - spent,
                    ProgressPercentage = budget.Amount > 0 ? (double)(spent / budget.Amount) : 0
                };

                budgetItems.Add(item);

                totalBudget += budget.Amount;
                totalSpent += spent;
            }

            // Update collection in one go
            _budgets.Clear();
            foreach (var item in budgetItems)
                _budgets.Add(item);
        }
        else
        {
            BudgetsSection.IsVisible = false;
            _budgets.Clear();
        }

        // Load recurring expenses - create new list to avoid flickering
        var expenseItems = new List<MonthlyExpenseItem>();
        if (activeExpenses.Any())
        {
            RecurringExpensesSection.IsVisible = true;

            foreach (var expense in activeExpenses.OrderBy(e => e.DayOfMonth))
            {
                var isPaid = await _paymentService.IsExpensePaidAsync(expense.Id, _currentMonth, _currentYear);

                var item = new MonthlyExpenseItem
                {
                    ExpenseId = expense.Id,
                    Name = expense.Name,
                    Description = expense.Description ?? "",
                    Amount = expense.Amount,
                    DayOfMonth = expense.DayOfMonth,
                    IsPaid = isPaid
                };

                expenseItems.Add(item);

                totalRecurringExpenses += expense.Amount;
                if (isPaid)
                    totalRecurringPaid += expense.Amount;
            }

            // Update collection in one go
            _expenses.Clear();
            foreach (var item in expenseItems)
                _expenses.Add(item);
        }
        else
        {
            RecurringExpensesSection.IsVisible = false;
            _expenses.Clear();
        }

        // Load one-time expenses - create new list to avoid flickering
        decimal totalOneTimeExpenses = 0;
        decimal totalOneTimeExpensesPaid = 0;
        var oneTimeExpenseItems = new List<OneTimeExpenseItem>();
        if (oneTimeExpenses.Any())
        {
            OneTimeExpensesCollection.IsVisible = true;

            foreach (var expense in oneTimeExpenses.OrderByDescending(e => e.Date))
            {
                var isPaid = await _paymentService.IsExpensePaidAsync(expense.Id, _currentMonth, _currentYear);

                var item = new OneTimeExpenseItem
                {
                    ExpenseId = expense.Id,
                    Name = expense.Name,
                    Description = expense.Description ?? "",
                    Amount = expense.Amount,
                    Date = expense.Date,
                    HasDescription = !string.IsNullOrWhiteSpace(expense.Description),
                    IsPaid = isPaid
                };

                oneTimeExpenseItems.Add(item);
                totalOneTimeExpenses += expense.Amount;
                if (isPaid)
                    totalOneTimeExpensesPaid += expense.Amount;
            }

            // Update collection in one go
            _oneTimeExpenses.Clear();
            foreach (var item in oneTimeExpenseItems)
                _oneTimeExpenses.Add(item);
        }
        else
        {
            OneTimeExpensesCollection.IsVisible = false;
            _oneTimeExpenses.Clear();
        }

        // Calculate totals (include paid one-time expenses in calculations)
        var grandTotal = totalBudget + totalRecurringExpenses + totalOneTimeExpenses;
        var grandSpent = totalSpent + totalRecurringPaid + totalOneTimeExpensesPaid;
        var grandRemaining = (totalBudget - totalSpent) + (totalRecurringExpenses - totalRecurringPaid) + (totalOneTimeExpenses - totalOneTimeExpensesPaid);

        // Load bank balance for current month
        var bankBalance = await _bankBalanceService.GetBankBalanceAsync(_currentMonth, _currentYear);

        // Calculate remaining after payments (Bank balance - To pay)
        var remainingAfterPayments = bankBalance.CurrentBalance - grandRemaining;

        // Update summary
        TotalLabel.Text = $"€ {grandTotal:N2}";
        PaidLabel.Text = $"€ {grandSpent:N2}";
        UnpaidLabel.Text = $"€ {grandRemaining:N2}";
        BankBalanceLabel.Text = $"€ {bankBalance.CurrentBalance:N2}";
        RemainingAfterPaymentsLabel.Text = $"€ {remainingAfterPayments:N2}";
    }

    private async Task UpdateSummaryOnly()
    {
        decimal totalBudget = 0;
        decimal totalSpent = 0;
        decimal totalRecurringExpenses = 0;
        decimal totalRecurringPaid = 0;
        decimal totalOneTimeExpenses = 0;

        // Calculate budget totals
        foreach (var item in _budgets)
        {
            totalBudget += item.BudgetAmount;
            totalSpent += item.SpentAmount;
        }

        // Calculate recurring expense totals
        foreach (var item in _expenses)
        {
            totalRecurringExpenses += item.Amount;
            if (item.IsPaid)
                totalRecurringPaid += item.Amount;
        }

        // Calculate one-time expense totals
        decimal totalOneTimeExpensesPaid = 0;
        foreach (var item in _oneTimeExpenses)
        {
            totalOneTimeExpenses += item.Amount;
            if (item.IsPaid)
                totalOneTimeExpensesPaid += item.Amount;
        }

        // Calculate totals (include paid one-time expenses)
        var grandTotal = totalBudget + totalRecurringExpenses + totalOneTimeExpenses;
        var grandSpent = totalSpent + totalRecurringPaid + totalOneTimeExpensesPaid;
        var grandRemaining = (totalBudget - totalSpent) + (totalRecurringExpenses - totalRecurringPaid) + (totalOneTimeExpenses - totalOneTimeExpensesPaid);

        // Load bank balance for current month
        var bankBalance = await _bankBalanceService.GetBankBalanceAsync(_currentMonth, _currentYear);

        // Calculate remaining after payments (Bank balance - To pay)
        var remainingAfterPayments = bankBalance.CurrentBalance - grandRemaining;

        // Update summary
        TotalLabel.Text = $"€ {grandTotal:N2}";
        PaidLabel.Text = $"€ {grandSpent:N2}";
        UnpaidLabel.Text = $"€ {grandRemaining:N2}";
        BankBalanceLabel.Text = $"€ {bankBalance.CurrentBalance:N2}";
        RemainingAfterPaymentsLabel.Text = $"€ {remainingAfterPayments:N2}";
    }

    private async void OnViewBudgetDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is MonthlyBudgetItem item)
        {
            await Navigation.PushAsync(new BudgetDetailPage(_transactionService, _budgetService, item.Budget, _currentMonth, _currentYear));
        }
    }

    private async void OnExpenseCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is MonthlyExpenseItem item)
        {
            if (e.Value)
            {
                await _paymentService.MarkAsPaidAsync(item.ExpenseId, _currentMonth, _currentYear);
            }
            else
            {
                await _paymentService.MarkAsUnpaidAsync(item.ExpenseId, _currentMonth, _currentYear);
            }

            // Update the item's IsPaid property
            item.IsPaid = e.Value;

            // Only update summary, don't reload entire list to prevent scroll jump
            await UpdateSummaryOnly();
        }
    }

    private async void OnOneTimeExpenseCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is OneTimeExpenseItem item)
        {
            if (e.Value)
            {
                await _paymentService.MarkAsPaidAsync(item.ExpenseId, _currentMonth, _currentYear);
            }
            else
            {
                await _paymentService.MarkAsUnpaidAsync(item.ExpenseId, _currentMonth, _currentYear);
            }

            // Update the item's IsPaid property
            item.IsPaid = e.Value;

            // Only update summary, don't reload entire list to prevent scroll jump
            await UpdateSummaryOnly();
        }
    }

    private async void OnEditBankBalanceClicked(object sender, EventArgs e)
    {
        var currentBalance = await _bankBalanceService.GetBankBalanceAsync(_currentMonth, _currentYear);

        var date = new DateTime(_currentYear, _currentMonth, 1);
        var monthName = date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("nl-NL"));

        string result = await DisplayPromptAsync(
            $"Banksaldo Bijwerken - {monthName}",
            $"Vul je banksaldo in voor {monthName}:",
            initialValue: currentBalance.CurrentBalance.ToString("F2"),
            keyboard: Keyboard.Numeric,
            placeholder: "0,00");

        if (!string.IsNullOrWhiteSpace(result))
        {
            if (decimal.TryParse(result, out decimal newBalance))
            {
                await _bankBalanceService.UpdateBankBalanceAsync(_currentMonth, _currentYear, newBalance);
                await LoadData();
            }
            else
            {
                await DisplayAlert("Fout", "Vul een geldig bedrag in", "OK");
            }
        }
    }

    private async void OnPreviousMonthClicked(object sender, EventArgs e)
    {
        _currentMonth--;
        if (_currentMonth < 1)
        {
            _currentMonth = 12;
            _currentYear--;
        }

        await LoadData();
    }

    private async void OnNextMonthClicked(object sender, EventArgs e)
    {
        _currentMonth++;
        if (_currentMonth > 12)
        {
            _currentMonth = 1;
            _currentYear++;
        }

        await LoadData();
    }

    private async void OnAddOneTimeExpenseClicked(object sender, EventArgs e)
    {
        var formPage = new OneTimeExpenseFormPage(_transactionService, _localization, _currentMonth, _currentYear);
        await Navigation.PushModalAsync(new NavigationPage(formPage));
    }

    private async void OnEditOneTimeExpenseClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is OneTimeExpenseItem item)
        {
            var formPage = new OneTimeExpenseFormPage(_transactionService, _localization, item.ExpenseId);
            await Navigation.PushModalAsync(new NavigationPage(formPage));
        }
    }

    private async void OnDeleteOneTimeExpenseClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is OneTimeExpenseItem item)
        {
            bool confirm = await DisplayAlert(
                "Verwijderen",
                $"Weet je zeker dat je '{item.Name}' wilt verwijderen?",
                "Ja",
                "Nee");

            if (confirm)
            {
                await _transactionService.DeleteExpenseAsync(item.ExpenseId);
                await LoadData();
            }
        }
    }
}

// Display model for monthly budget items
public class MonthlyBudgetItem
{
    public Budget Budget { get; set; } = null!;
    public Guid BudgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public double ProgressPercentage { get; set; }
}

// Display model for monthly expense items
public class MonthlyExpenseItem
{
    public Guid ExpenseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DayOfMonth { get; set; }
    public bool IsPaid { get; set; }
}

// Display model for one-time expense items
public class OneTimeExpenseItem
{
    public Guid ExpenseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public bool HasDescription { get; set; }
    public bool IsPaid { get; set; }
}
