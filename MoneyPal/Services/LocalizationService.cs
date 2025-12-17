using System.Globalization;
using System.Text.Json;

namespace MoneyPal.Services;

public class LocalizationService : ILocalizationService
{
    private Dictionary<string, object> _translations = new();
    private string _currentLanguage = "en-US";

    public string CurrentLanguage => _currentLanguage;
    public event EventHandler? LanguageChanged;

    public LocalizationService()
    {
        LoadLanguage(_currentLanguage);
    }

    public void SetLanguage(string languageCode)
    {
        if (_currentLanguage == languageCode) return;

        _currentLanguage = languageCode;
        LoadLanguage(languageCode);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
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
            var fileName = $"Resources/Localization/{languageCode}.json";
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
