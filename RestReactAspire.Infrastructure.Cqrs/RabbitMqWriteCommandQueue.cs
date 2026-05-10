using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RestReactAspire.Infrastructure.Cqrs;

public sealed class RabbitMqWriteCommandQueue : IWriteCommandQueue
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;

    public RabbitMqWriteCommandQueue(RabbitMqConnectionManager connectionManager, IOptions<RabbitMqOptions> options)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
    }

    public Task EnqueueAsync(WriteCommandEnvelope command, CancellationToken cancellationToken = default)
    {
        return EnqueueCoreAsync(command, cancellationToken);
    }

    private async Task EnqueueCoreAsync(WriteCommandEnvelope command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(command);
        var body = Encoding.UTF8.GetBytes(payload);

        using var channel = await _connectionManager.GetConnection()
            .CreateChannelAsync(options: default, cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            _options.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            _options.QueueName,
            _options.ExchangeName,
            routingKey: _options.QueueName,
            arguments: null,
            noWait: false,
            cancellationToken: cancellationToken);

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: _options.QueueName,
            mandatory: false,
            basicProperties: new BasicProperties { Persistent = true },
            body: body,
            cancellationToken: cancellationToken);
    }
}
