using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RestReactAspire.Shared.Cqrs;

public sealed class RabbitMqWriteCommandProcessor : BackgroundService
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly WriteCommandHandler _handler;
    private readonly WriteCommandResultCoordinator _resultCoordinator;
    private readonly ILogger<RabbitMqWriteCommandProcessor> _logger;

    public RabbitMqWriteCommandProcessor(
        RabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        WriteCommandHandler handler,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<RabbitMqWriteCommandProcessor> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _handler = handler;
        _resultCoordinator = resultCoordinator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var channel = await _connectionManager.GetConnection()
                    .CreateChannelAsync(options: default, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(
                    _options.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    passive: false,
                    noWait: false,
                    cancellationToken: stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(_options.QueueName, autoAck: true, cancellationToken: stoppingToken);
                    if (delivery is null)
                    {
                        await Task.Delay(100, stoppingToken);
                        continue;
                    }

                    WriteCommandEnvelope? command = null;
                    WriteCommandResult result;
                    try
                    {
                        var payload = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        command = JsonSerializer.Deserialize<WriteCommandEnvelope>(payload);
                        if (command is null)
                        {
                            _logger.LogWarning("Received empty or invalid write command payload");
                            continue;
                        }

                        result = _handler.Handle(command);
                    }
                    catch (Exception ex)
                    {
                        result = WriteCommandResult.Failure("UnhandledCommandError", ex.Message);
                        _logger.LogError(ex, "Failed to process write command from queue {QueueName}", _options.QueueName);
                    }

                    if (command is not null)
                    {
                        _resultCoordinator.Complete(command.CommandId, result);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Write command processor failed; retrying in 2 seconds");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
