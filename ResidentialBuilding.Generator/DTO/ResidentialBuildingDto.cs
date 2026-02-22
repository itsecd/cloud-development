namespace Generator.DTO;

/// <summary>
///     DTO объекта жилого строительства.
/// </summary>
public class ResidentialBuildingDto
{
    /// <summary>
    ///     Идентификатор в системе.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Адрес.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    ///     Тип недвижимости.
    /// </summary>
    public string PropertyType { get; set; } = string.Empty;

    /// <summary>
    ///     Год постройки.
    /// </summary>
    public int BuildYear { get; set; }

    /// <summary>
    ///     Общая площадь.
    /// </summary>
    public double TotalArea { get; set; }

    /// <summary>
    ///     Жилая площадь.
    /// </summary>
    public double LivingArea { get; set; }

    /// <summary>
    ///     Этаж.
    /// </summary>
    public int? Floor { get; set; }

    /// <summary>
    ///     Этажность.
    /// </summary>
    public int TotalFloors { get; set; }

    /// <summary>
    ///     Кадастровый номер.
    /// </summary>
    public string CadastralNumber { get; set; } = string.Empty;

    /// <summary>
    ///     Кадастровая стоимость.
    /// </summary>
    public decimal CadastralValue { get; set; }
}