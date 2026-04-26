using Ocelot.Errors;

namespace AspireApp.ApiGateway.LoadBalancing;

public class ServicesAreEmptyError(string message) : Error(message, OcelotErrorCode.ServicesAreEmptyError, 503);