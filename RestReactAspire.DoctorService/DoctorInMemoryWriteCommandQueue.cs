using RestReactAspire.Shared.Cqrs;

namespace RestReactAspire.DoctorService;

public sealed class DoctorInMemoryWriteCommandQueue : IWriteCommandQueue
{
    private readonly DoctorWriteCommandHandler _handler;
    private readonly WriteCommandResultCoordinator _resultCoordinator;

    public DoctorInMemoryWriteCommandQueue(DoctorWriteCommandHandler handler, WriteCommandResultCoordinator resultCoordinator)
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
