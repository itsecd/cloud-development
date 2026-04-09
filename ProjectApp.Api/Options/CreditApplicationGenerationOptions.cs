namespace ProjectApp.Api.Options;

public class CreditApplicationGenerationOptions
{
    public const string SectionName = "CreditApplicationSettings";

    public double CentralBankKeyRate { get; set; } = 21.0;
    public decimal MinRequestedAmount { get; set; } = 50_000m;
    public decimal MaxRequestedAmount { get; set; } = 5_000_000m;
    public int MinTermMonths { get; set; } = 6;
    public int MaxTermMonths { get; set; } = 360;
    public int MaxApplicationAgeYears { get; set; } = 2;
    public string[] CreditTypes { get; set; } =
    [
        "Потребительский",
        "Ипотека",
        "Автокредит",
        "Рефинансирование",
        "Кредитная карта"
    ];
}
