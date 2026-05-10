namespace File.Service.Storage;

/// <summary>
/// Обеспечивает сохранение и чтение файлов сотрудников из объектного хранилища.
/// </summary>
public interface IEmployeeFileStorage
{
    /// <summary>
    /// Сохраняет JSON-файл сотрудника в объектное хранилище.
    /// </summary>
    Task SaveEmployeeJsonAsync(int employeeId, string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Пытается прочитать JSON-файл сотрудника из объектного хранилища.
    /// </summary>
    Task<string?> TryReadEmployeeJsonAsync(int employeeId, CancellationToken cancellationToken = default);
}
