using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Application.Services;
using OpenIddict.Abstractions;

namespace AuthPlaypen.Api.Infrastructure.OpenIddict;

public sealed class OpenIddictScopeSyncOrchestrator(IOpenIddictScopeManager scopeManager) : IOpenIddictSyncOrchestrator<ScopeDto>
{
    public async Task HandleCreationAsync(ScopeDto dto, CancellationToken cancellationToken)
    {
        var descriptor = ToDescriptor(dto);
        await scopeManager.CreateAsync(descriptor, cancellationToken);
    }

    public async Task HandleUpdateAsync(ScopeDto dto, CancellationToken cancellationToken)
    {
        var scope = await scopeManager.FindByNameAsync(dto.ScopeName, cancellationToken);
        if (scope is null)
        {
            await HandleCreationAsync(dto, cancellationToken);
            return;
        }

        var descriptor = ToDescriptor(dto);
        await scopeManager.UpdateAsync(scope, descriptor, cancellationToken);
    }

    public async Task HandleDeletionAsync(Guid scopeId, CancellationToken cancellationToken)
    {
        var scope = await scopeManager.FindByIdAsync(scopeId.ToString(), cancellationToken);
        if (scope is null)
        {
            return;
        }

        await scopeManager.DeleteAsync(scope, cancellationToken);
    }

    private static OpenIddictScopeDescriptor ToDescriptor(ScopeDto dto)
    {
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = dto.ScopeName,
            DisplayName = dto.DisplayName,
            Description = dto.Description
        };

        foreach (var application in dto.Applications)
        {
            descriptor.Resources.Add(application.ClientId);
        }

        return descriptor;
    }
}
