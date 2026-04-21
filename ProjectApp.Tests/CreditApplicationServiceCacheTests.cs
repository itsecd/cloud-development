using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProjectApp.Api.Options;
using ProjectApp.Api.Services.CreditApplicationService;

namespace ProjectApp.Tests;

public class CreditApplicationServiceCacheTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnSamePayload_ForRepeatedId()
    {
        var generator = new CreditApplicationGenerator(Options.Create(new CreditApplicationGenerationOptions()));
        var validator = new CreditApplicationValidator();
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new CreditApplicationService(cache, generator, validator, NullLogger<CreditApplicationService>.Instance);

        var first = await service.GetByIdAsync(777);
        var second = await service.GetByIdAsync(777);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.CreditType, second.CreditType);
        Assert.Equal(first.RequestedAmount, second.RequestedAmount);
        Assert.Equal(first.TermMonths, second.TermMonths);
        Assert.Equal(first.InterestRate, second.InterestRate);
        Assert.Equal(first.ApplicationDate, second.ApplicationDate);
        Assert.Equal(first.RequiresInsurance, second.RequiresInsurance);
        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.DecisionDate, second.DecisionDate);
        Assert.Equal(first.ApprovedAmount, second.ApprovedAmount);
    }
}
