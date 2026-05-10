using System.Collections.Concurrent;

namespace RestReactAspire.Infrastructure.Cqrs;

public sealed class WriteCommandResultCoordinator
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<WriteCommandResult>> _pending = new();

    public void Prepare(Guid commandId)
    {
        _pending.TryAdd(commandId, new TaskCompletionSource<WriteCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    public void Complete(Guid commandId, WriteCommandResult result)
    {
        if (_pending.TryGetValue(commandId, out var source))
        {
            source.TrySetResult(result);
        }
    }

    public async Task<WriteCommandResult> WaitAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        if (!_pending.TryGetValue(commandId, out var source))
        {
            return WriteCommandResult.Failure("CommandNotPrepared", $"Command {commandId} was not prepared before waiting.");
        }

        try
        {
            return await source.Task.WaitAsync(_defaultTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return WriteCommandResult.Failure("Timeout", $"Write command {commandId} timed out.");
        }
        catch (OperationCanceledException)
        {
            return WriteCommandResult.Failure("Cancelled", $"Write command {commandId} was cancelled.");
        }
        finally
        {
            _pending.TryRemove(commandId, out _);
        }
    }
}
