using AuthPlaypen.Application.Dtos;

namespace AuthPlaypen.Application.Services;

public sealed class NoOpOpenIddictApplicationSyncService : IOpenIddictSyncOrchestrator<ApplicationDto>
{
    public Task HandleCreationAsync(ApplicationDto dto, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task HandleUpdateAsync(ApplicationDto dto, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task HandleDeletionAsync(Guid applicationId, CancellationToken cancellationToken) => Task.CompletedTask;
}
