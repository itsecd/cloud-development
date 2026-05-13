using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Domain.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class AwsServiceExtensions
{
    public static IServiceCollection AddSnsPublishing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceUrl = configuration["AWS:ServiceURL"];

        if (!string.IsNullOrEmpty(serviceUrl))
        {
            services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
                new AmazonSimpleNotificationServiceClient(
                    new BasicAWSCredentials("test", "test"),
                    new AmazonSimpleNotificationServiceConfig { ServiceURL = serviceUrl }));
        }
        else
        {
            services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
                new AmazonSimpleNotificationServiceClient());
        }

        services.AddScoped<ISnsPublisherService, SnsPublisherService>();
        return services;
    }
}
