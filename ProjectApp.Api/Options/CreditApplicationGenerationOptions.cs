namespace ProjectApp.Api.Options;

/// <summary>
/// Настройки генерации кредитных заявок.
/// </summary>
public class CreditApplicationGenerationOptions
{
    /// <summary>Имя секции в appsettings.</summary>
    public const string SectionName = "CreditApplicationSettings";

    /// <summary>Минимальная допустимая ставка (ключевая ставка ЦБ).</summary>
    public double CentralBankKeyRate { get; set; } = 21.0;
    /// <summary>Минимальная запрашиваемая сумма.</summary>
    public decimal MinRequestedAmount { get; set; } = 50_000m;
    /// <summary>Максимальная запрашиваемая сумма.</summary>
    public decimal MaxRequestedAmount { get; set; } = 5_000_000m;
    /// <summary>Минимальный срок кредита.</summary>
    public int MinTermMonths { get; set; } = 6;
    /// <summary>Максимальный срок кредита.</summary>
    public int MaxTermMonths { get; set; } = 360;
    /// <summary>Максимальный возраст даты подачи заявки в годах.</summary>
    public int MaxApplicationAgeYears { get; set; } = 2;
    /// <summary>Справочник доступных типов кредита.</summary>
    public string[] CreditTypes { get; set; } =
    [
        "Потребительский",
        "Ипотека",
        "Автокредит",
        "Рефинансирование",
        "Кредитная карта"
    ];
}
