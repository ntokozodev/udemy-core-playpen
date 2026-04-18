using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Application.Services;
using AuthPlaypen.Domain.Entities;
using OpenIddict.Abstractions;

namespace AuthPlaypen.Api.Services;

public sealed class OpenIddictApplicationSyncService(IOpenIddictApplicationManager applicationManager) : IOpenIddictSyncOrchestrator<ApplicationDto>
{
    public async Task HandleCreationAsync(ApplicationDto dto, CancellationToken cancellationToken)
    {
        var descriptor = ToDescriptor(dto);
        await applicationManager.CreateAsync(descriptor, cancellationToken);
    }

    public async Task HandleUpdateAsync(ApplicationDto dto, CancellationToken cancellationToken)
    {
        var application = await applicationManager.FindByClientIdAsync(dto.ClientId, cancellationToken);
        if (application is null)
        {
            await HandleCreationAsync(dto, cancellationToken);
            return;
        }

        var descriptor = ToDescriptor(dto);
        await applicationManager.UpdateAsync(application, descriptor, cancellationToken);
    }

    public async Task HandleDeletionAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await applicationManager.FindByIdAsync(applicationId.ToString(), cancellationToken);
        if (application is null)
        {
            return;
        }

        await applicationManager.DeleteAsync(application, cancellationToken);
    }

    private static OpenIddictApplicationDescriptor ToDescriptor(ApplicationDto dto)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = dto.ClientId,
            ClientSecret = dto.ClientSecret,
            DisplayName = dto.DisplayName,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
            ClientType = dto.Flow == ApplicationFlow.AuthorizationWithPKCE
                ? OpenIddictConstants.ClientTypes.Public
                : OpenIddictConstants.ClientTypes.Confidential
        };

        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);

        if (dto.Flow == ApplicationFlow.AuthorizationWithPKCE)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);

            AddUriList(dto.RedirectUris, descriptor.RedirectUris);
            AddUriList(dto.PostLogoutRedirectUris, descriptor.PostLogoutRedirectUris);
        }
        else
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
        }

        foreach (var scope in dto.Scopes.Select(s => s.ScopeName).Distinct())
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
        }

        return descriptor;
    }

    private static void AddUriList(string? urisValue, ISet<Uri> target)
    {
        if (string.IsNullOrWhiteSpace(urisValue))
        {
            return;
        }

        var uris = urisValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(uri => new Uri(uri, UriKind.Absolute));

        foreach (var uri in uris)
        {
            target.Add(uri);
        }
    }
}
