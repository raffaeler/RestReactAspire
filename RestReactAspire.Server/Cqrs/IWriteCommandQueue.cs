namespace RestReactAspire.Server.Cqrs;

public interface IWriteCommandQueue
{
    Task EnqueueAsync(WriteCommandEnvelope command, CancellationToken cancellationToken = default);
}
