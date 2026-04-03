using AuthPlaypen.Application.Dtos;

namespace AuthPlaypen.Application.Services;

public sealed class NoOpOpenIddictScopeSyncService : IOpenIddictSyncOrchestrator<ScopeDto>
{
    public Task HandleCreationAsync(ScopeDto dto, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task HandleUpdateAsync(ScopeDto dto, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task HandleDeletionAsync(Guid scopeId, CancellationToken cancellationToken) => Task.CompletedTask;
}

