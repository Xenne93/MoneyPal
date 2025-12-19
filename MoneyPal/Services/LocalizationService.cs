using System.Globalization;
using System.Text.Json;

namespace MoneyPal.Services;

public class LocalizationService : ILocalizationService
{
    private Dictionary<string, object> _translations = new();
    private Dictionary<string, Dictionary<string, object>> _languageCache = new();
    private string _currentLanguage = "en-US";
    private const string LanguagePreferenceKey = "AppLanguage";
    private readonly List<string> _supportedLanguages = new() { "nl-NL", "en-US", "de-DE" };

    public string CurrentLanguage => _currentLanguage;
    public event EventHandler? LanguageChanged;
    public List<string> SupportedLanguages => _supportedLanguages;

    public LocalizationService()
    {
        // Try to load saved language preference
        var savedLanguage = Preferences.Get(LanguagePreferenceKey, string.Empty);

        if (!string.IsNullOrEmpty(savedLanguage) && _supportedLanguages.Contains(savedLanguage))
        {
            _currentLanguage = savedLanguage;
        }
        else
        {
            // Detect system language
            _currentLanguage = DetectSystemLanguage();
        }

        Console.WriteLine($"LocalizationService initialized with language: {_currentLanguage}");
        LoadLanguage(_currentLanguage);
    }

    private string DetectSystemLanguage()
    {
        try
        {
            // Get system culture
            var systemCulture = CultureInfo.CurrentUICulture;
            var languageCode = systemCulture.Name; // e.g., "nl-NL", "en-US", "en-GB"

            Console.WriteLine($"Detected system language: {languageCode}");

            // Check if exact match exists
            if (_supportedLanguages.Contains(languageCode))
            {
                return languageCode;
            }

            // Check for language-only match (e.g., "nl" matches "nl-NL")
            var languageOnly = languageCode.Split('-')[0];
            var matchingLanguage = _supportedLanguages.FirstOrDefault(lang => lang.StartsWith(languageOnly + "-"));

            if (matchingLanguage != null)
            {
                Console.WriteLine($"Found matching language: {matchingLanguage} for {languageCode}");
                return matchingLanguage;
            }

            // Fallback to English
            Console.WriteLine($"No match found for {languageCode}, falling back to en-US");
            return "en-US";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error detecting system language: {ex.Message}");
            return "en-US";
        }
    }

    public void SetLanguage(string languageCode)
    {
        if (_currentLanguage == languageCode) return;

        if (!_supportedLanguages.Contains(languageCode))
        {
            Console.WriteLine($"Language {languageCode} not supported, ignoring");
            return;
        }

        _currentLanguage = languageCode;

        // Save language preference
        Preferences.Set(LanguagePreferenceKey, languageCode);
        Console.WriteLine($"Language changed to: {languageCode}");

        LoadLanguage(languageCode);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetLanguageDisplayName(string languageCode)
    {
        try
        {
            // Check if we already have this language cached
            if (!_languageCache.ContainsKey(languageCode))
            {
                // Load the language file to get the display name
                var fileName = $"Localization/{languageCode}.json";
                using var stream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                var translations = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (translations != null)
                {
                    _languageCache[languageCode] = translations;
                }
            }

            // Get the Language.Name from the cached translations
            if (_languageCache.TryGetValue(languageCode, out var langTranslations))
            {
                var languageName = GetNestedValueFromDict(langTranslations, "Language.Name");
                if (!string.IsNullOrEmpty(languageName))
                {
                    return languageName;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading language display name for {languageCode}: {ex.Message}");
        }

        // Fallback to language code if we can't find the name
        return languageCode;
    }

    private string? GetNestedValueFromDict(Dictionary<string, object> dict, string key)
    {
        var keys = key.Split('.');
        object? current = dict;

        foreach (var k in keys)
        {
            if (current is Dictionary<string, object> d && d.TryGetValue(k, out var value))
            {
                current = value;
            }
            else if (current is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(k, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current switch
        {
            string str => str,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => null
        };
    }

    public string GetString(string key)
    {
        return GetNestedValue(key) ?? key;
    }

    public string GetString(string key, params object[] args)
    {
        var value = GetNestedValue(key) ?? key;
        return string.Format(value, args);
    }

    private void LoadLanguage(string languageCode)
    {
        try
        {
            var fileName = $"Localization/{languageCode}.json";
            using var stream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            _translations = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading language file: {ex.Message}");
            _translations = new Dictionary<string, object>();
        }
    }

    private string? GetNestedValue(string key)
    {
        var keys = key.Split('.');
        object? current = _translations;

        foreach (var k in keys)
        {
            if (current is Dictionary<string, object> dict && dict.TryGetValue(k, out var value))
            {
                current = value;
            }
            else if (current is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(k, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current switch
        {
            string str => str,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => null
        };
    }
}
