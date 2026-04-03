namespace AuthPlaypen.Application.Services;

public interface IOpenIddictSyncOrchestrator<TDto>
{
    Task HandleCreationAsync(TDto dto, CancellationToken cancellationToken);
    Task HandleUpdateAsync(TDto dto, CancellationToken cancellationToken);
    Task HandleDeletionAsync(Guid id, CancellationToken cancellationToken);
}
