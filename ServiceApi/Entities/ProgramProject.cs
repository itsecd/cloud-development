using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace ServiceApi.Entities;

public class ProgramProject
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Название проекта
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Заказчик проекта
    /// </summary>
    [JsonPropertyName("customer")]
    public string Customer { get; set; }

    /// <summary>
    /// Менеджер проекта
    /// </summary>
    [JsonPropertyName("manager")]
    public string Manager { get; set; }

    /// <summary>
    /// Дата начала
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Плановая дата завершения
    /// </summary>
    [JsonPropertyName("planEndDate")]
    public DateOnly PlanEndDate { get; set; }

    /// <summary>
    /// Фактическая дата завершения
    /// </summary>
    [JsonPropertyName("actualEndDate")]
    public DateOnly? ActualEndDate { get; set; }

    /// <summary>
    /// Бюджет
    /// </summary>
    [JsonPropertyName("budget")]
    public decimal Budget { get; set; }

    /// <summary>
    /// Фактические затраты
    /// </summary>
    [JsonPropertyName("actualCost")]
    public decimal ActualCost { get; set; }

    /// <summary>
    /// Процент выполнения
    /// </summary>
    [JsonPropertyName("percentComplete")]
    public int PercentComplete { get; set; }
}