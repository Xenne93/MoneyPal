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
    private readonly MonthInitializationService _monthInitService;
    private readonly DataStorageService _dataStorage;

    private int _currentMonth;
    private int _currentYear;
    private bool _isLoadingData = false;
    private ObservableCollection<MonthlyBudgetItem> _budgets = new();
    private ObservableCollection<MonthlyExpenseItem> _expenses = new();
    private ObservableCollection<OneTimeExpenseItem> _oneTimeExpenses = new();

    public MonthlyOverviewPage(IBudgetService budgetService, ITransactionService transactionService,
        IExpenseService expenseService, IPaymentService paymentService, IBankBalanceService bankBalanceService,
        ILocalizationService localization, MonthInitializationService monthInitService, DataStorageService dataStorage)
    {
        InitializeComponent();
        _budgetService = budgetService;
        _transactionService = transactionService;
        _expenseService = expenseService;
        _paymentService = paymentService;
        _bankBalanceService = bankBalanceService;
        _localization = localization;
        _monthInitService = monthInitService;
        _dataStorage = dataStorage;

        // Set to current month
        _currentMonth = DateTime.Now.Month;
        _currentYear = DateTime.Now.Year;

        BudgetsCollection.ItemsSource = _budgets;
        ExpensesCollection.ItemsSource = _expenses;
        OneTimeExpensesCollection.ItemsSource = _oneTimeExpenses;

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        // Update localized texts
        UpdateLocalizedTexts();
    }

    private void UpdateLocalizedTexts()
    {
        // Update page title
        Title = _localization.GetString("MonthlyOverview.Title");

        // Update summary card headers
        TotalExpensesHeaderLabel.Text = _localization.GetString("MonthlyOverview.TotalExpenses");
        SpentHeaderLabel.Text = _localization.GetString("MonthlyOverview.Spent");
        ToPayHeaderLabel.Text = _localization.GetString("MonthlyOverview.ToPay");
        BankBalanceHeaderLabel.Text = _localization.GetString("MonthlyOverview.BankBalance");

        // Update remaining after payments card
        RemainingAfterPaymentsHeaderLabel.Text = _localization.GetString("MonthlyOverview.RemainingAfterPayments");
        BankBalanceMinusToPayLabel.Text = _localization.GetString("MonthlyOverview.BankBalanceMinusToPay");

        // Update empty state
        NoDataLabel.Text = _localization.GetString("MonthlyOverview.NoData");
        CreateBudgetsAndExpensesLabel.Text = _localization.GetString("MonthlyOverview.CreateBudgetsAndExpenses");

        // Update section headers
        BudgetsSectionHeaderLabel.Text = _localization.GetString("MonthlyOverview.Budgets");
        RecurringExpensesSectionHeaderLabel.Text = _localization.GetString("MonthlyOverview.RecurringExpenses");
        OneTimeExpensesSectionHeaderLabel.Text = _localization.GetString("MonthlyOverview.OneTimeExpenses");
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLocalizedTexts();
        await LoadData(); // Reload data to update all texts with new language
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await LoadData();
        }
        catch (Exception ex)
        {
            // Log error to console (works on all platforms)
            System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex}");
            Console.WriteLine($"Error in OnAppearing: {ex}");

            // Show error dialog on main thread (cross-platform)
            if (MainThread.IsMainThread)
            {
                await DisplayAlert(
                    _localization.GetString("Common.Error"),
                    ex.Message,
                    _localization.GetString("Common.OK"));
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert(
                        _localization.GetString("Common.Error"),
                        ex.Message,
                        _localization.GetString("Common.OK"));
                });
            }
        }
    }

    private async Task LoadData()
    {
        try
        {
            _isLoadingData = true;

            // Update month label with localized culture
            var date = new DateTime(_currentYear, _currentMonth, 1);
            var culture = new CultureInfo(_localization.CurrentLanguage);
            MonthLabel.Text = date.ToString("MMMM yyyy", culture);

            // Check if month is initialized
            var isInitialized = await _monthInitService.IsMonthInitializedAsync(_currentMonth, _currentYear);

            if (!isInitialized)
            {
                // Show "Not Initialized" state
                NotInitializedState.IsVisible = true;
                RegenerateMonthSection.IsVisible = false;
                EmptyState.IsVisible = false;
                BudgetsSection.IsVisible = false;
                RecurringExpensesSection.IsVisible = false;
                OneTimeExpensesSection.IsVisible = false;
                OneTimeExpensesCollection.IsVisible = false;

                // Reset button state
                InitializeMonthButton.IsEnabled = true;
                InitializeMonthButton.Text = "Begin met invullen";
                return;
            }

            // Month is initialized, hide not initialized state and show regenerate button
            NotInitializedState.IsVisible = false;
            RegenerateMonthSection.IsVisible = true;

            // Get budget snapshots for this month
            var budgetSnapshots = await _dataStorage.GetMonthlyBudgetSnapshotsAsync(_currentMonth, _currentYear);

            // Get recurring expense snapshots for this month
            var expenseSnapshots = await _dataStorage.GetMonthlyRecurringExpenseSnapshotsAsync(_currentMonth, _currentYear);

            // Get one-time expenses for this month
            var oneTimeExpenses = await _transactionService.GetOneTimeExpensesForMonthAsync(_currentMonth, _currentYear);
            if (oneTimeExpenses == null) oneTimeExpenses = new List<Expense>();

        // Check if we have any data
        if (!budgetSnapshots.Any() && !expenseSnapshots.Any() && !oneTimeExpenses.Any())
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

        // Load budget snapshots - create new list to avoid flickering
        var budgetItems = new List<MonthlyBudgetItem>();
        if (budgetSnapshots.Any())
        {
            BudgetsSection.IsVisible = true;

            foreach (var snapshot in budgetSnapshots.OrderBy(b => b.Name))
            {
                // Get spending from actual Expense records linked to the original budget
                var spent = await _transactionService.GetTotalForBudgetAsync(snapshot.OriginalBudgetId, _currentMonth, _currentYear);

                // Try to get the actual budget for navigation purposes, fallback to creating a temporary one from snapshot
                var actualBudget = await _budgetService.GetBudgetByIdAsync(snapshot.OriginalBudgetId);
                if (actualBudget == null)
                {
                    // Create a temporary budget object from the snapshot
                    actualBudget = new Budget
                    {
                        Id = snapshot.OriginalBudgetId,
                        Name = snapshot.Name,
                        Amount = snapshot.Amount,
                        CategoryId = snapshot.CategoryId,
                        Description = snapshot.Description,
                        CountAsFixedExpense = snapshot.CountAsFixedExpense,
                        IsActive = false // Mark as inactive since it doesn't exist in master data
                    };
                }

                var item = new MonthlyBudgetItem
                {
                    Budget = actualBudget,
                    BudgetId = snapshot.OriginalBudgetId,
                    Name = snapshot.Name,
                    BudgetAmount = snapshot.Amount,
                    SpentAmount = spent,
                    RemainingAmount = snapshot.Amount - spent,
                    ProgressPercentage = snapshot.Amount > 0 ? (double)(spent / snapshot.Amount) : 0
                };

                budgetItems.Add(item);

                totalBudget += snapshot.Amount;
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

        // Load recurring expense snapshots - create new list to avoid flickering
        var expenseItems = new List<MonthlyExpenseItem>();
        if (expenseSnapshots.Any())
        {
            RecurringExpensesSection.IsVisible = true;

            foreach (var snapshot in expenseSnapshots.OrderBy(e => e.DayOfMonth))
            {
                // Check payment status using the original expense ID
                var isPaid = await _paymentService.IsExpensePaidAsync(snapshot.OriginalExpenseId, _currentMonth, _currentYear);

                var item = new MonthlyExpenseItem
                {
                    ExpenseId = snapshot.OriginalExpenseId,
                    Name = snapshot.Name,
                    Description = snapshot.Description ?? "",
                    Amount = snapshot.Amount,
                    DayOfMonth = snapshot.DayOfMonth,
                    IsPaid = isPaid
                };

                expenseItems.Add(item);

                totalRecurringExpenses += snapshot.Amount;
                if (isPaid)
                    totalRecurringPaid += snapshot.Amount;
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
                    Name = expense.Name ?? "",
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
        if (bankBalance == null)
        {
            System.Diagnostics.Debug.WriteLine("Warning: bankBalance is null, creating default");
            bankBalance = new BankBalance { CurrentBalance = 0 };
        }

        // Calculate remaining after payments (Bank balance - To pay)
        var remainingAfterPayments = bankBalance.CurrentBalance - grandRemaining;

        // Update summary
        TotalLabel.Text = $"€ {grandTotal:N2}";
        PaidLabel.Text = $"€ {grandSpent:N2}";
        UnpaidLabel.Text = $"€ {grandRemaining:N2}";
        BankBalanceLabel.Text = $"€ {bankBalance.CurrentBalance:N2}";
        RemainingAfterPaymentsLabel.Text = $"€ {remainingAfterPayments:N2}";
        }
        catch (Exception ex)
        {
            // Log the error with full details (works on all platforms)
            System.Diagnostics.Debug.WriteLine($"Error in LoadData: {ex}");
            Console.WriteLine($"Error in LoadData: {ex}");

            // Re-throw so OnAppearing can handle it
            throw;
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private async Task UpdateSummaryOnly()
    {
        try
        {
            decimal totalBudget = 0;
            decimal totalSpent = 0;
            decimal totalRecurringExpenses = 0;
            decimal totalRecurringPaid = 0;
            decimal totalOneTimeExpenses = 0;

            // Calculate budget totals
            if (_budgets != null)
            {
                foreach (var item in _budgets)
                {
                    totalBudget += item.BudgetAmount;
                    totalSpent += item.SpentAmount;
                }
            }

            // Calculate recurring expense totals
            if (_expenses != null)
            {
                foreach (var item in _expenses)
                {
                    totalRecurringExpenses += item.Amount;
                    if (item.IsPaid)
                        totalRecurringPaid += item.Amount;
                }
            }

            // Calculate one-time expense totals
            decimal totalOneTimeExpensesPaid = 0;
            if (_oneTimeExpenses != null)
            {
                foreach (var item in _oneTimeExpenses)
                {
                    totalOneTimeExpenses += item.Amount;
                    if (item.IsPaid)
                        totalOneTimeExpensesPaid += item.Amount;
                }
            }

            // Calculate totals (include paid one-time expenses)
            var grandTotal = totalBudget + totalRecurringExpenses + totalOneTimeExpenses;
            var grandSpent = totalSpent + totalRecurringPaid + totalOneTimeExpensesPaid;
            var grandRemaining = (totalBudget - totalSpent) + (totalRecurringExpenses - totalRecurringPaid) + (totalOneTimeExpenses - totalOneTimeExpensesPaid);

            // Load bank balance for current month
            var bankBalance = await _bankBalanceService.GetBankBalanceAsync(_currentMonth, _currentYear);
            if (bankBalance == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: bankBalance is null in UpdateSummaryOnly");
                bankBalance = new BankBalance { CurrentBalance = 0 };
            }

            // Calculate remaining after payments (Bank balance - To pay)
            var remainingAfterPayments = bankBalance.CurrentBalance - grandRemaining;

            // Update summary
            TotalLabel.Text = $"€ {grandTotal:N2}";
            PaidLabel.Text = $"€ {grandSpent:N2}";
            UnpaidLabel.Text = $"€ {grandRemaining:N2}";
            BankBalanceLabel.Text = $"€ {bankBalance.CurrentBalance:N2}";
            RemainingAfterPaymentsLabel.Text = $"€ {remainingAfterPayments:N2}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in UpdateSummaryOnly: {ex}");
            Console.WriteLine($"Error in UpdateSummaryOnly: {ex}");
        }
    }

    private async void OnViewBudgetDetailsClicked(object sender, EventArgs e)
    {
        MonthlyBudgetItem? item = null;

        // Support both Button and TapGestureRecognizer
        if (sender is Button button && button.CommandParameter is MonthlyBudgetItem buttonItem)
        {
            item = buttonItem;
        }
        else if (sender is Border border && border.GestureRecognizers.Count > 0)
        {
            var tapGesture = border.GestureRecognizers[0] as TapGestureRecognizer;
            item = tapGesture?.CommandParameter as MonthlyBudgetItem;
        }

        if (item != null)
        {
            await Navigation.PushAsync(new BudgetDetailPage(_transactionService, _budgetService, _bankBalanceService, _localization, item.Budget, _currentMonth, _currentYear));
        }
    }

    private async void OnExpenseCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is MonthlyExpenseItem item)
        {
            // Don't show popup if we're just loading data
            if (_isLoadingData)
            {
                return;
            }

            if (e.Value)
            {
                // Ask user if they want to deduct from bank balance
                bool deductFromBalance = await DisplayAlert(
                    _localization.GetString("MonthlyOverview.DeductFromBalance"),
                    $"{_localization.GetString("MonthlyOverview.DeductFromBalanceQuestion")} (€ {item.Amount:N2})?",
                    _localization.GetString("Common.Yes"),
                    _localization.GetString("Common.No"));

                await _paymentService.MarkAsPaidAsync(item.ExpenseId, _currentMonth, _currentYear);

                // If yes, deduct from bank balance
                if (deductFromBalance)
                {
                    var currentBalance = await _bankBalanceService.GetBankBalanceAsync(_currentMonth, _currentYear);
                    var newBalance = currentBalance.CurrentBalance - item.Amount;
                    await _bankBalanceService.UpdateBankBalanceAsync(_currentMonth, _currentYear, newBalance);
                }
            }
            else
            {
                // Ask user if they want to add back to bank balance
                bool addBackToBalance = await DisplayAlert(
                    _localization.GetString("MonthlyOverview.AddBackToBalance"),
                    $"{_localization.GetString("MonthlyOverview.AddBackToBalanceQuestion")} (€ {item.Amount:N2})?",
                    _localization.GetString("Common.Yes"),
                    _localization.GetString("Common.No"));

                await _paymentService.MarkAsUnpaidAsync(item.ExpenseId, _currentMonth, _currentYear);

                // If yes, add back to bank balance
                if (addBackToBalance)
                {
                    var currentBalance = await _bankBalanceService.GetBankBalanceAsync(_currentMonth, _currentYear);
                    var newBalance = currentBalance.CurrentBalance + item.Amount;
                    await _bankBalanceService.UpdateBankBalanceAsync(_currentMonth, _currentYear, newBalance);
                }
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
            // Don't show popup if we're just loading data
            if (_isLoadingData)
            {
                return;
            }

            if (e.Value)
            {
                // Ask user if they want to deduct from bank balance
                bool deductFromBalance = await DisplayAlert(
                    _localization.GetString("MonthlyOverview.DeductFromBalance"),
                    $"{_localization.GetString("MonthlyOverview.DeductFromBalanceQuestion")} (€ {item.Amount:N2})?",
                    _localization.GetString("Common.Yes"),
                    _localization.GetString("Common.No"));

                await _paymentService.MarkAsPaidAsync(item.ExpenseId, _currentMonth, _currentYear);

                // If yes, deduct from bank balance
                if (deductFromBalance)
                {
                    var currentBalance = await _bankBalanceService.GetBankBalanceAsync(_currentMonth, _currentYear);
                    var newBalance = currentBalance.CurrentBalance - item.Amount;
                    await _bankBalanceService.UpdateBankBalanceAsync(_currentMonth, _currentYear, newBalance);
                }
            }
            else
            {
                // Ask user if they want to add back to bank balance
                bool addBackToBalance = await DisplayAlert(
                    _localization.GetString("MonthlyOverview.AddBackToBalance"),
                    $"{_localization.GetString("MonthlyOverview.AddBackToBalanceQuestion")} (€ {item.Amount:N2})?",
                    _localization.GetString("Common.Yes"),
                    _localization.GetString("Common.No"));

                await _paymentService.MarkAsUnpaidAsync(item.ExpenseId, _currentMonth, _currentYear);

                // If yes, add back to bank balance
                if (addBackToBalance)
                {
                    var currentBalance = await _bankBalanceService.GetBankBalanceAsync(_currentMonth, _currentYear);
                    var newBalance = currentBalance.CurrentBalance + item.Amount;
                    await _bankBalanceService.UpdateBankBalanceAsync(_currentMonth, _currentYear, newBalance);
                }
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
        var culture = new System.Globalization.CultureInfo(_localization.CurrentLanguage);
        var monthName = date.ToString("MMMM yyyy", culture);

        string result = await DisplayPromptAsync(
            $"{_localization.GetString("MonthlyOverview.UpdateBankBalance")} - {monthName}",
            $"{_localization.GetString("MonthlyOverview.EnterBankBalanceFor")} {monthName}:",
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
                await DisplayAlert(
                    _localization.GetString("Common.Error"),
                    _localization.GetString("MonthlyOverview.EnterValidAmount"),
                    _localization.GetString("Common.OK"));
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
        OneTimeExpenseItem? item = null;

        // Support both Button and TapGestureRecognizer
        if (sender is Button button && button.CommandParameter is OneTimeExpenseItem buttonItem)
        {
            item = buttonItem;
        }
        else if (sender is Border border && border.GestureRecognizers.Count > 0)
        {
            var tapGesture = border.GestureRecognizers[0] as TapGestureRecognizer;
            item = tapGesture?.CommandParameter as OneTimeExpenseItem;
        }

        if (item != null)
        {
            var formPage = new OneTimeExpenseFormPage(_transactionService, _localization, item.ExpenseId);
            await Navigation.PushModalAsync(new NavigationPage(formPage));
        }
    }

    private async void OnDeleteOneTimeExpenseClicked(object sender, EventArgs e)
    {
        OneTimeExpenseItem? item = null;

        // Support both Button and TapGestureRecognizer
        if (sender is Button button && button.CommandParameter is OneTimeExpenseItem buttonItem)
        {
            item = buttonItem;
        }
        else if (sender is Border border && border.GestureRecognizers.Count > 0)
        {
            var tapGesture = border.GestureRecognizers[0] as TapGestureRecognizer;
            item = tapGesture?.CommandParameter as OneTimeExpenseItem;
        }

        if (item != null)
        {
            bool confirm = await DisplayAlert(
                _localization.GetString("Common.Delete"),
                $"{_localization.GetString("Common.Delete")} '{item.Name}'?",
                _localization.GetString("Common.Yes"),
                _localization.GetString("Common.No"));

            if (confirm)
            {
                await _transactionService.DeleteExpenseAsync(item.ExpenseId);
                await LoadData();
            }
        }
    }

    private async void OnInitializeMonthClicked(object sender, EventArgs e)
    {
        try
        {
            var date = new DateTime(_currentYear, _currentMonth, 1);
            var culture = new CultureInfo(_localization.CurrentLanguage);
            var monthName = date.ToString("MMMM yyyy", culture);

            bool confirm = await DisplayAlert(
                "Maand initialiseren",
                $"Wil je {monthName} initialiseren met je huidige budgetten, vaste lasten en inkomsten?",
                "Ja",
                "Nee");

            if (confirm)
            {
                // Show loading indicator
                InitializeMonthButton.IsEnabled = false;
                InitializeMonthButton.Text = "Bezig met initialiseren...";

                await _monthInitService.InitializeMonthAsync(_currentMonth, _currentYear);

                // Reload data
                await LoadData();

                await DisplayAlert(
                    "Succes",
                    $"{monthName} is succesvol geïnitialiseerd!",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing month: {ex}");
            await DisplayAlert(
                "Fout",
                $"Er is een fout opgetreden bij het initialiseren van de maand: {ex.Message}",
                "OK");

            // Re-enable button
            InitializeMonthButton.IsEnabled = true;
            InitializeMonthButton.Text = "Begin met invullen";
        }
    }

    private async void OnRegenerateMonthClicked(object sender, EventArgs e)
    {
        try
        {
            var date = new DateTime(_currentYear, _currentMonth, 1);
            var culture = new CultureInfo(_localization.CurrentLanguage);
            var monthName = date.ToString("MMMM yyyy", culture);

            // Ask user whether to preserve data or reset
            var action = await DisplayActionSheet(
                $"Opnieuw genereren: {monthName}",
                "Annuleren",
                null,
                "Behoud handmatige wijzigingen",
                "Reset volledig (verwijder alles)");

            if (action == "Annuleren" || action == null)
                return;

            bool preserveUserData = action == "Behoud handmatige wijzigingen";

            bool confirm = await DisplayAlert(
                "Bevestiging",
                preserveUserData
                    ? $"De budgetten en vaste lasten van {monthName} worden bijgewerkt naar de huidige versies. Je handmatige uitgaven en betalingsstatussen blijven behouden."
                    : $"WAARSCHUWING: Alle data van {monthName} wordt verwijderd en opnieuw gegenereerd. Dit kan niet ongedaan gemaakt worden!",
                "Doorgaan",
                "Annuleren");

            if (confirm)
            {
                // Show loading indicator
                RegenerateMonthButton.IsEnabled = false;
                RegenerateMonthButton.Text = "Bezig...";

                await _monthInitService.RegenerateMonthAsync(_currentMonth, _currentYear, preserveUserData);

                // Reload data
                await LoadData();

                await DisplayAlert(
                    "Succes",
                    $"{monthName} is succesvol opnieuw gegenereerd!",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error regenerating month: {ex}");
            await DisplayAlert(
                "Fout",
                $"Er is een fout opgetreden bij het opnieuw genereren van de maand: {ex.Message}",
                "OK");
        }
        finally
        {
            // Re-enable button
            if (RegenerateMonthButton != null)
            {
                RegenerateMonthButton.IsEnabled = true;
                RegenerateMonthButton.Text = "Opnieuw genereren";
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
