using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RestReactAspire.Infrastructure.Cqrs;

public abstract class RabbitMqWriteCommandProcessorBase : BackgroundService
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly IWriteCommandHandler _handler;
    private readonly WriteCommandResultCoordinator _resultCoordinator;
    private readonly ILogger _logger;

    protected RabbitMqWriteCommandProcessorBase(
        RabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        IWriteCommandHandler handler,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger logger)
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

                await channel.ExchangeDeclareAsync(
                    _options.ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    passive: false,
                    noWait: false,
                    cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    _options.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    passive: false,
                    noWait: false,
                    cancellationToken: stoppingToken);

                await channel.QueueBindAsync(
                    _options.QueueName,
                    _options.ExchangeName,
                    routingKey: _options.QueueName,
                    arguments: null,
                    noWait: false,
                    cancellationToken: stoppingToken);

                // Bind to admin reset fanout exchange for broadcast reset commands
                await channel.ExchangeDeclareAsync(
                    _options.AdminResetExchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    passive: false,
                    noWait: false,
                    cancellationToken: stoppingToken);

                await channel.QueueBindAsync(
                    _options.QueueName,
                    _options.AdminResetExchangeName,
                    routingKey: string.Empty,
                    arguments: null,
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
