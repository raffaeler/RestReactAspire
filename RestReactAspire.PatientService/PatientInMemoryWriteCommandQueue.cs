using RestReactAspire.Infrastructure.Cqrs;

namespace RestReactAspire.PatientService;

public sealed class PatientInMemoryWriteCommandQueue : IWriteCommandQueue
{
    private readonly IWriteCommandHandler _handler;
    private readonly WriteCommandResultCoordinator _resultCoordinator;

    public PatientInMemoryWriteCommandQueue(IWriteCommandHandler handler, WriteCommandResultCoordinator resultCoordinator)
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
