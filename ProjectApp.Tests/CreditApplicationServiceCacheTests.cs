using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:ExpirationMinutes"] = "10"
            })
            .Build();
        var service = new CreditApplicationService(cache, generator, configuration, NullLogger<CreditApplicationService>.Instance);

        var first = await service.GetByIdAsync(777);
        var second = await service.GetByIdAsync(777);

        Assert.Equivalent(first, second);
    }
}
