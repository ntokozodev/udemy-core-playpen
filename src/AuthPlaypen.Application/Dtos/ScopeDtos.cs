namespace AuthPlaypen.Application.Dtos;

public record ApplicationReferenceDto(Guid Id, string DisplayName, string ClientId);

public record ScopeDto(
    Guid Id,
    string DisplayName,
    string ScopeName,
    string Description,
    IReadOnlyCollection<ApplicationReferenceDto> Applications,
    EntityMetadataDto Metadata);

public record CreateScopeRequest(
    string DisplayName,
    string ScopeName,
    string Description,
    IReadOnlyCollection<Guid>? ApplicationIds);

public record UpdateScopeRequest(
    string DisplayName,
    string ScopeName,
    string Description,
    IReadOnlyCollection<Guid>? ApplicationIds);
