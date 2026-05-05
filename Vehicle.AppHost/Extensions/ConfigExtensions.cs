using Microsoft.Extensions.Configuration;

namespace Vehicle.AppHost.Extensions;

/// <summary>
/// Методы расширения для безопасного чтения обязательных значений конфигурации
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Получает обязательное строковое значение из конфигурации
    /// </summary>
    public static string GetRequiredString(this IConfiguration configuration, string key)
    {
        return configuration[key] ?? throw new InvalidOperationException($"{key} is not configured.");
    }

    /// <summary>
    /// Получает обязательное целочисленное значение из конфигурации
    /// </summary>
    public static int GetRequiredInt(this IConfiguration configuration, string key)
    {
        return configuration.GetValue<int?>(key) ?? throw new InvalidOperationException($"{key} is not configured.");
    }

    /// <summary>
    /// Получает обязательный массив целых чисел из конфигурации
    /// </summary>
    public static int[] GetRequiredIntArray(this IConfiguration configuration, string key)
    {
        return configuration.GetSection(key).Get<int[]>() ?? throw new InvalidOperationException($"{key} is not configured.");
    }
}