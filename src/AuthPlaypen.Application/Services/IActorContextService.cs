namespace AuthPlaypen.Application.Services;

public interface IActorContextService
{
    ActorContext GetCurrentActor();
}

public sealed record ActorContext(string DisplayName, string? Email, bool IsSystem);
