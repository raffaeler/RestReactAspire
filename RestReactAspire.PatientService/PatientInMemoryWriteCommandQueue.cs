using RestReactAspire.Shared.Cqrs;

namespace RestReactAspire.PatientService;

public sealed class PatientInMemoryWriteCommandQueue : IWriteCommandQueue
{
    private readonly PatientWriteCommandHandler _handler;
    private readonly WriteCommandResultCoordinator _resultCoordinator;

    public PatientInMemoryWriteCommandQueue(PatientWriteCommandHandler handler, WriteCommandResultCoordinator resultCoordinator)
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
