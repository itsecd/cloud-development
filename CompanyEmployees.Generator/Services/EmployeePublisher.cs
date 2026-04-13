using CompanyEmployees.Generator.Models;
using MassTransit;

namespace CompanyEmployees.Generator.Services;

public class EmployeePublisher(
    ISendEndpointProvider sendEndpointProvider,
    ILogger<EmployeePublisher> logger) : IEmployeePublisher
{
    public Task PublishAsync(CompanyEmployeeModel employee, CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            try
            {
                var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:employees"));
                await sendEndpoint.Send(employee, cancellationToken);

                logger.LogInformation("Sent employee {EmployeeId} to SQS queue", employee.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send employee {EmployeeId} to SQS", employee.Id);
            }
        }, cancellationToken);
    }
}
