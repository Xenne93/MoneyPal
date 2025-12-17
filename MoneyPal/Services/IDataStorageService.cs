namespace MoneyPal.Services;

public interface IDataStorageService
{
    Task<T?> LoadAsync<T>(string key) where T : class;
    Task SaveAsync<T>(string key, T data) where T : class;
    Task DeleteAsync(string key);
}
