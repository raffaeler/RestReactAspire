using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestReactAspire.Infrastructure.Cqrs;

namespace RestReactAspire.PatientService;

public sealed class PatientRabbitMqWriteCommandProcessor : RabbitMqWriteCommandProcessorBase
{
    public PatientRabbitMqWriteCommandProcessor(
        RabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        IWriteCommandHandler handler,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<PatientRabbitMqWriteCommandProcessor> logger)
        : base(connectionManager, options, handler, resultCoordinator, logger)
    {
    }
}
