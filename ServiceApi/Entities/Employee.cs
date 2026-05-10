using System.Text.Json.Serialization;

namespace Service.Api.Entities;

/// <summary>
/// Сотрудник компании.
/// </summary>
public sealed class Employee
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    [JsonPropertyName("hireDate")]
    public DateOnly HireDate { get; set; }

    [JsonPropertyName("salary")]
    public decimal Salary { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("isFired")]
    public bool IsFired { get; set; }

    [JsonPropertyName("fireDate")]
    public DateOnly? FireDate { get; set; }
}
