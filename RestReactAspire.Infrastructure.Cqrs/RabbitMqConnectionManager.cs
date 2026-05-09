using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RestReactAspire.Infrastructure.Cqrs;

public sealed class RabbitMqConnectionManager : IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly object _syncLock = new();
    private IConnection? _connection;

    public RabbitMqConnectionManager(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public IConnection GetConnection()
    {
        lock (_syncLock)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            return _connection;
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
