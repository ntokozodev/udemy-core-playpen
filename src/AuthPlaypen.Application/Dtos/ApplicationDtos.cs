using AuthPlaypen.Domain.Entities;

namespace AuthPlaypen.Application.Dtos;

public record EntityMetadataDto(string CreatedBy, DateTimeOffset CreatedAt, string UpdatedBy, DateTimeOffset UpdatedAt);

public record ScopeReferenceDto(Guid Id, string DisplayName, string ScopeName, string Description);

public record ApplicationDto(
    Guid Id,
    string DisplayName,
    string ClientId,
    string ClientSecret,
    ApplicationFlow Flow,
    string? PostLogoutRedirectUris,
    string? RedirectUris,
    IReadOnlyCollection<ScopeReferenceDto> Scopes,
    EntityMetadataDto Metadata);

public record CreateApplicationRequest(
    string DisplayName,
    string ClientId,
    string ClientSecret,
    ApplicationFlow Flow,
    string? PostLogoutRedirectUris,
    string? RedirectUris,
    IReadOnlyCollection<Guid> ScopeIds);

public record UpdateApplicationRequest(
    string DisplayName,
    string ClientId,
    string ClientSecret,
    ApplicationFlow Flow,
    string? PostLogoutRedirectUris,
    string? RedirectUris,
    IReadOnlyCollection<Guid> ScopeIds);
