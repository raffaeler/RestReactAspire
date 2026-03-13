namespace RestReactAspire.Server.Cqrs;

public sealed class InMemoryWriteCommandQueue : IWriteCommandQueue
{
    private readonly WriteCommandHandler _handler;
    private readonly WriteCommandResultCoordinator _resultCoordinator;

    public InMemoryWriteCommandQueue(WriteCommandHandler handler, WriteCommandResultCoordinator resultCoordinator)
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
