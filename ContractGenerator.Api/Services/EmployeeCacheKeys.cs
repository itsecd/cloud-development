namespace ContractGenerator.Api.Services;

/// <summary>
/// Утилита для построения ключей кэша сотрудников.
/// </summary>
public static class EmployeeCacheKeys
{
    /// <summary>
    /// Создает ключ кэша для сотрудника с указанным идентификатором.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    public static string ForId(int id) => $"employee:{id}";
}
