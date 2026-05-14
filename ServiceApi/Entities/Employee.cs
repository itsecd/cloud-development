using System.Text.Json.Serialization;

namespace Service.Api.Entities;

/// <summary>
/// Сотрудник компании.
/// </summary>
public sealed class Employee
{
    /// <summary>
    /// Уникальный идентификатор сотрудника.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Полное имя сотрудника.
    /// </summary>
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Должность сотрудника.
    /// </summary>
    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Подразделение, в котором работает сотрудник.
    /// </summary>
    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Дата приёма сотрудника на работу.
    /// </summary>
    [JsonPropertyName("hireDate")]
    public DateOnly HireDate { get; set; }

    /// <summary>
    /// Текущий размер заработной платы сотрудника.
    /// </summary>
    [JsonPropertyName("salary")]
    public decimal Salary { get; set; }

    /// <summary>
    /// Корпоративный адрес электронной почты сотрудника.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Контактный телефон сотрудника.
    /// </summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Признак увольнения сотрудника.
    /// </summary>
    [JsonPropertyName("isFired")]
    public bool IsFired { get; set; }

    /// <summary>
    /// Дата увольнения сотрудника, если сотрудник уволен.
    /// </summary>
    [JsonPropertyName("fireDate")]
    public DateOnly? FireDate { get; set; }
}