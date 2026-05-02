using System.Text.Json.Serialization;

namespace Service.Api.Entities;

/// <summary>
/// Сотрудник компании.
/// </summary>
public sealed class Employee
{
    /// <summary>
    /// Идентификатор сотрудника в системе.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Фамилия, имя и отчество сотрудника.
    /// </summary>
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Должность сотрудника.
    /// </summary>
    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Отдел, в котором работает сотрудник.
    /// </summary>
    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Дата приема сотрудника на работу.
    /// </summary>
    [JsonPropertyName("hireDate")]
    public DateOnly HireDate { get; set; }

    /// <summary>
    /// Текущий оклад сотрудника.
    /// </summary>
    [JsonPropertyName("salary")]
    public decimal Salary { get; set; }

    /// <summary>
    /// Электронная почта сотрудника.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Контактный номер телефона сотрудника.
    /// </summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Признак увольнения сотрудника.
    /// </summary>
    [JsonPropertyName("isFired")]
    public bool IsFired { get; set; }

    /// <summary>
    /// Дата увольнения сотрудника, если он уволен.
    /// </summary>
    [JsonPropertyName("fireDate")]
    public DateOnly? FireDate { get; set; }
}
