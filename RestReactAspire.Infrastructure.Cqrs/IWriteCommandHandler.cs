namespace RestReactAspire.Infrastructure.Cqrs;

public interface IWriteCommandHandler
{
    WriteCommandResult Handle(WriteCommandEnvelope envelope);
}
