using System.Text.Json;
using RestReactAspire.Infrastructure.Cqrs;

namespace RestReactAspire.StatisticsService;

public sealed class StatisticsWriteCommandHandler : IWriteCommandHandler
{
    public WriteCommandResult Handle(WriteCommandEnvelope envelope)
    {
        return envelope.CommandType switch
        {
            nameof(SeedDataCommand) => HandleSeedData(),
            nameof(ResetDataCommand) => HandleResetData(),
            _ => WriteCommandResult.Failure("UnknownCommand", $"Unsupported command type {envelope.CommandType}"),
        };
    }

    private WriteCommandResult HandleSeedData()
    {
        // Statistics aggregates from HTTP — no local seed data needed.
        // This is a no-op; actual seeding is done by the endpoint via HTTP fan-out.
        return WriteCommandResult.Success();
    }

    private WriteCommandResult HandleResetData()
    {
        // Statistics aggregates from HTTP — no local data to reset.
        // This is a no-op; actual reset is done by the endpoint via HTTP fan-out.
        return WriteCommandResult.Success();
    }
}
