using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestReactAspire.Infrastructure.Cqrs;

namespace RestReactAspire.ExamService;

public sealed class ExamRabbitMqWriteCommandProcessor : RabbitMqWriteCommandProcessorBase
{
    public ExamRabbitMqWriteCommandProcessor(
        RabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        IWriteCommandHandler handler,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<ExamRabbitMqWriteCommandProcessor> logger)
        : base(connectionManager, options, handler, resultCoordinator, logger)
    {
    }
}
