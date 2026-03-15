using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectApp.Api.Services.CreditApplicationGeneratorService;
using ProjectApp.Domain.Entities;
using Xunit;

namespace ProjectApp.Tests;

public class CreditApplicationGeneratorTests
{
    private CreditApplicationGenerator CreateGenerator(double minRate = 16.0)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FinanceSettings:MinInterestRatePercent"] = minRate.ToString(System.Globalization.CultureInfo.InvariantCulture)
            })
            .Build();

        return new CreditApplicationGenerator(config, NullLogger<CreditApplicationGenerator>.Instance);
    }

    [Fact]
    public void InterestRate_NotLessThan_MinRate_And_Rounded()
    {
        var generator = CreateGenerator(14.5);
        var app = generator.Generate();
        Assert.True(app.InterestRate >= 14.5);
        Assert.Equal(Math.Round(app.InterestRate, 2), app.InterestRate);
    }

    [Fact]
    public void ApprovedAmount_Only_For_Approved_Status_And_Leq_Requested()
    {
        var generator = CreateGenerator();
        var app = generator.Generate();

        if (app.Status == "Одобрена")
        {
            Assert.NotNull(app.ApprovedAmount);
            Assert.True(app.ApprovedAmount!.Value <= app.RequestedAmount);
            Assert.Equal(Math.Round(app.ApprovedAmount.Value, 2), app.ApprovedAmount.Value);
        }
        else
        {
            Assert.Null(app.ApprovedAmount);
        }
    }

    [Fact]
    public void DecisionDate_Only_For_Terminal_Statuses_And_After_ApplicationDate()
    {
        var generator = CreateGenerator();
        var app = generator.Generate();

        if (app.Status is "Одобрена" or "Отклонена")
        {
            Assert.NotNull(app.DecisionDate);
            Assert.True(app.DecisionDate!.Value >= app.ApplicationDate);
        }
        else
        {
            Assert.Null(app.DecisionDate);
        }
    }

    [Fact]
    public void ApplicationDate_No_More_Than_Two_Years_Ago()
    {
        var generator = CreateGenerator();
        var app = generator.Generate();
        var twoYearsAgo = DateOnly.FromDateTime(DateTime.Now.AddYears(-2));
        Assert.True(app.ApplicationDate >= twoYearsAgo);
        Assert.True(app.ApplicationDate <= DateOnly.FromDateTime(DateTime.Now));
    }
}
