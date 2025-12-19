namespace MoneyPal.Services;

public interface ILocalizationService
{
    string GetString(string key);
    string GetString(string key, params object[] args);
    void SetLanguage(string languageCode);
    string GetLanguageDisplayName(string languageCode);
    string CurrentLanguage { get; }
    List<string> SupportedLanguages { get; }
    event EventHandler? LanguageChanged;
}
