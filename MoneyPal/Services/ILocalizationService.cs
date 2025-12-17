namespace MoneyPal.Services;

public interface ILocalizationService
{
    string GetString(string key);
    string GetString(string key, params object[] args);
    void SetLanguage(string languageCode);
    string CurrentLanguage { get; }
    event EventHandler? LanguageChanged;
}
