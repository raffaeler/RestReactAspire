using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestReactAspire.Infrastructure.Cqrs;

namespace RestReactAspire.StatisticsService;

public sealed class StatisticsRabbitMqWriteCommandProcessor : RabbitMqWriteCommandProcessorBase
{
    public StatisticsRabbitMqWriteCommandProcessor(
        RabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        IWriteCommandHandler handler,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<StatisticsRabbitMqWriteCommandProcessor> logger)
        : base(connectionManager, options, handler, resultCoordinator, logger)
    {
    }
}
