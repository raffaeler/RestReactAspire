using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestReactAspire.Infrastructure.Cqrs;

namespace RestReactAspire.DoctorService;

public sealed class DoctorRabbitMqWriteCommandProcessor : RabbitMqWriteCommandProcessorBase
{
    public DoctorRabbitMqWriteCommandProcessor(
        RabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        IWriteCommandHandler handler,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<DoctorRabbitMqWriteCommandProcessor> logger)
        : base(connectionManager, options, handler, resultCoordinator, logger)
    {
    }
}
