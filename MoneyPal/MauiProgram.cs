using Microsoft.Extensions.Logging;
using MoneyPal.Services;
using MoneyPal.Pages;

namespace MoneyPal;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        // Register services
        builder.Services.AddSingleton<DataStorageService>();
        builder.Services.AddSingleton<IDataStorageService>(sp => sp.GetRequiredService<DataStorageService>());
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<IExpenseService, ExpenseService>();
        builder.Services.AddSingleton<IPaymentService, PaymentService>();
        builder.Services.AddSingleton<IBudgetService, BudgetService>();
        builder.Services.AddSingleton<ITransactionService, TransactionService>();
        builder.Services.AddSingleton<IIncomeService, IncomeService>();
        builder.Services.AddSingleton<IBankBalanceService, BankBalanceService>();
        builder.Services.AddSingleton<MonthInitializationService>();

        // Register App and AppShell
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<AppShell>();

        // Register pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<RecurringExpensesPage>();
        builder.Services.AddTransient<IncomesPage>();
        builder.Services.AddTransient<MonthlyOverviewPage>();
        builder.Services.AddTransient<BudgetsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<OneTimeExpenseFormPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}