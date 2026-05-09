namespace RestReactAspire.Infrastructure.Cqrs;

public sealed class InMemoryWriteCommandQueue : IWriteCommandQueue
{
    private readonly IWriteCommandHandler _handler;
    private readonly WriteCommandResultCoordinator _resultCoordinator;

    public InMemoryWriteCommandQueue(IWriteCommandHandler handler, WriteCommandResultCoordinator resultCoordinator)
    {
        _handler = handler;
        _resultCoordinator = resultCoordinator;
    }

    public Task EnqueueAsync(WriteCommandEnvelope command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _handler.Handle(command);
        _resultCoordinator.Complete(command.CommandId, result);
        return Task.CompletedTask;
    }
}
