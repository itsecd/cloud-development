using Service.Api.Entity;

namespace Service.Api.Broker;

public interface IProducerService
{
    public Task SendMessage(ProgramProject pp);
}
