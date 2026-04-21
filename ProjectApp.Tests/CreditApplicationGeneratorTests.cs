using Microsoft.Extensions.Options;
using ProjectApp.Api.Options;
using ProjectApp.Api.Services.CreditApplicationService;

namespace ProjectApp.Tests;

public class CreditApplicationGeneratorTests
{
    private readonly CreditApplicationGenerationOptions _options = new();

    [Fact]
    public void Generate_ShouldProduceApplicationMatchingRequiredRules()
    {
        var generator = CreateGenerator();
        var validator = new CreditApplicationValidator();
        var application = generator.Generate();

        Assert.Contains(application.CreditType, _options.CreditTypes);
        Assert.InRange(application.RequestedAmount, _options.MinRequestedAmount, _options.MaxRequestedAmount);
        Assert.InRange(application.TermMonths, _options.MinTermMonths, _options.MaxTermMonths);
        Assert.True(application.InterestRate >= _options.CentralBankKeyRate);
        Assert.Equal(Math.Round(application.InterestRate, 2), application.InterestRate);
        Assert.True(application.ApplicationDate >= DateOnly.FromDateTime(DateTime.Today.AddYears(-_options.MaxApplicationAgeYears)));
        Assert.True(application.ApplicationDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

        Assert.True(validator.TryValidate(application, out var error), error);
    }

    [Fact]
    public void Generate_MultipleTimes_ShouldKeepInvariants()
    {
        var generator = CreateGenerator();
        var validator = new CreditApplicationValidator();

        for (var i = 0; i < 200; i++)
        {
            var application = generator.Generate();
            Assert.True(validator.TryValidate(application, out var error), $"Iteration {i}: {error}");
        }
    }

    private CreditApplicationGenerator CreateGenerator() => new(Options.Create(_options));
}
