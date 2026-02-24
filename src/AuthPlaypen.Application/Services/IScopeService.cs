using AuthPlaypen.Application.Dtos;

namespace AuthPlaypen.Application.Services;

public interface IScopeService
{
    Task<CursorPagedResultDto<ScopeDto>> GetPageAsync(Guid? cursor, int pageSize, CancellationToken cancellationToken);
    Task<ScopeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ScopeReferenceDto>> SearchAsync(string searchTerm, int pageSize, CancellationToken cancellationToken);
    Task<(ScopeDto? Scope, string? Error)> CreateAsync(CreateScopeRequest request, CancellationToken cancellationToken);
    Task<(ScopeDto? Scope, string? Error, bool NotFound)> UpdateAsync(Guid id, UpdateScopeRequest request, CancellationToken cancellationToken);
    Task<(bool Deleted, string? Error, bool NotFound)> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
