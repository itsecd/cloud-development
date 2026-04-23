using System.Text.Json.Serialization;

namespace Service.Api.Entities;

/// <summary>
/// Сотрудник компании
/// </summary>
public class Employee
{
    /// <summary>
    /// Идентификатор сотрудника в системе
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// ФИО
    /// </summary>
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    /// <summary>
    /// Должность
    /// </summary>
    [JsonPropertyName("position")]
    public string? Position { get; set; }

    /// <summary>
    /// Отдел
    /// </summary>
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    /// <summary>
    /// Дата приема
    /// </summary>
    [JsonPropertyName("hireDate")]
    public DateOnly HireDate { get; set; }

    /// <summary>
    /// Оклад
    /// </summary>
    [JsonPropertyName("salary")]
    public decimal Salary { get; set; }

    /// <summary>
    /// Электронная почта
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Номер телефона
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Индикатор увольнения
    /// </summary>
    [JsonPropertyName("isFired")]
    public bool IsFired { get; set; }

    /// <summary>
    /// Дата увольнения
    /// </summary>
    [JsonPropertyName("fireDate")]
    public DateOnly? FireDate { get; set; }
}