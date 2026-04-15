using Ocelot.Errors;

namespace ApiGateway.LoadBalancing;

public sealed class ServicesAreEmptyError(string message)
    : Error(message, OcelotErrorCode.UnableToFindDownstreamRouteError, 503);
