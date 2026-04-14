namespace ResidentialBuilding.EventSink.Service.Messaging;

public interface ISubscriptionService
{
    /// <summary>
    /// Делает попытку подписаться на топик SNS
    /// </summary>
    public Task SubscribeEndpoint();
}