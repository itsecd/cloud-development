namespace ContractGenerator.Shared.Storage;

/// <summary>
/// Формирует имена JSON-файлов сотрудников в объектном хранилище.
/// </summary>
public static class EmployeeFileKeys
{
    /// <summary>
    /// Создает ключ файла для сотрудника.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    public static string ForId(int id) => $"employee_{id}.json";
}
