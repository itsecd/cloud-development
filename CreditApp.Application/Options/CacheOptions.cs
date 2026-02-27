namespace CreditApp.Application.Options;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public int AbsoluteExpirationMinutes { get; set; } = 10;
}
