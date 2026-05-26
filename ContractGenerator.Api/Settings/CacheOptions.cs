namespace ContractGenerator.Api.Settings;

/// <summary>
/// Настройки кэширования сотрудников компании.
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Время жизни записи о сотруднике в кэше, в минутах.
    /// </summary>
    public int EmployeeTtlMinutes { get; init; } = 15;
}
