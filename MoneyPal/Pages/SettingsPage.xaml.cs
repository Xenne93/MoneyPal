using MoneyPal.Services;

namespace MoneyPal.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly ILocalizationService _localization;
    private readonly DataStorageService _dataStorage;

    public SettingsPage(ILocalizationService localization, DataStorageService dataStorage)
    {
        InitializeComponent();
        _localization = localization;
        _dataStorage = dataStorage;

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        UpdateLabels();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        // Update all labels with localized text
        Title = _localization.GetString("Settings.Title");
        GeneralSectionLabel.Text = _localization.GetString("Settings.General");
        LanguageLabel.Text = _localization.GetString("Settings.Language");
        AboutSectionLabel.Text = _localization.GetString("Settings.About");
        VersionLabel.Text = _localization.GetString("Settings.Version");

        // Danger Zone section
        DangerZoneSectionLabel.Text = _localization.GetString("Settings.DangerZone");
        ClearDatabaseLabel.Text = _localization.GetString("Settings.ClearDatabase");
        ClearDatabaseDescriptionLabel.Text = _localization.GetString("Settings.ClearDatabaseDescription");

        // Update current language display
        CurrentLanguageLabel.Text = _localization.GetLanguageDisplayName(_localization.CurrentLanguage);
    }

    private async void OnLanguageSettingTapped(object sender, EventArgs e)
    {
        try
        {
            var supportedLanguages = _localization.SupportedLanguages;
            var languageNames = supportedLanguages.Select(lang => _localization.GetLanguageDisplayName(lang)).ToArray();

            var selectedLanguageName = await DisplayActionSheet(
                _localization.GetString("Settings.SelectLanguage"),
                _localization.GetString("Common.Cancel"),
                null,
                languageNames);

            if (string.IsNullOrEmpty(selectedLanguageName) ||
                selectedLanguageName == _localization.GetString("Common.Cancel"))
            {
                return;
            }

            // Find the language code for the selected language name
            var selectedLanguageCode = supportedLanguages.FirstOrDefault(lang =>
                _localization.GetLanguageDisplayName(lang) == selectedLanguageName);

            if (!string.IsNullOrEmpty(selectedLanguageCode) &&
                selectedLanguageCode != _localization.CurrentLanguage)
            {
                // Change language
                _localization.SetLanguage(selectedLanguageCode);

                // Update labels immediately
                UpdateLabels();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing language: {ex}");
            await DisplayAlert(
                _localization.GetString("Common.Error"),
                ex.Message,
                _localization.GetString("Common.OK"));
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLabels();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _localization.LanguageChanged -= OnLanguageChanged;
    }

    private async void OnClearDatabaseTapped(object sender, EventArgs e)
    {
        try
        {
            // First confirmation
            bool firstConfirm = await DisplayAlert(
                _localization.GetString("Settings.ClearDatabase"),
                _localization.GetString("Settings.ClearDatabaseWarning"),
                _localization.GetString("Common.Yes"),
                _localization.GetString("Common.No"));

            if (!firstConfirm)
                return;

            // Second confirmation (extra safety)
            bool secondConfirm = await DisplayAlert(
                _localization.GetString("Settings.AreYouSure"),
                _localization.GetString("Settings.ClearDatabaseFinalWarning"),
                _localization.GetString("Settings.DeleteEverything"),
                _localization.GetString("Common.Cancel"));

            if (!secondConfirm)
                return;

            // Clear the database
            await _dataStorage.ClearAllDataAsync();

            // Show success message
            await DisplayAlert(
                _localization.GetString("Common.Success"),
                _localization.GetString("Settings.DatabaseCleared"),
                _localization.GetString("Common.OK"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing database: {ex}");
            await DisplayAlert(
                _localization.GetString("Common.Error"),
                $"{_localization.GetString("Settings.ClearDatabaseError")}: {ex.Message}",
                _localization.GetString("Common.OK"));
        }
    }
}
