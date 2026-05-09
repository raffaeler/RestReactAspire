namespace RestReactAspire.Shared.Cqrs;

public interface IWriteCommandQueue
{
    Task EnqueueAsync(WriteCommandEnvelope command, CancellationToken cancellationToken = default);
}
