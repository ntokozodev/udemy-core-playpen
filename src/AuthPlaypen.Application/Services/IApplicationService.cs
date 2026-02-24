using AuthPlaypen.Application.Dtos;

namespace AuthPlaypen.Application.Services;

public interface IApplicationService
{
    Task<CursorPagedResultDto<ApplicationDto>> GetPageAsync(Guid? cursor, int pageSize, CancellationToken cancellationToken);
    Task<ApplicationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ApplicationReferenceDto>> SearchAsync(string searchTerm, int pageSize, CancellationToken cancellationToken);
    Task<(ApplicationDto? Application, string? Error)> CreateAsync(CreateApplicationRequest request, CancellationToken cancellationToken);
    Task<(ApplicationDto? Application, string? Error, bool NotFound)> UpdateAsync(Guid id, UpdateApplicationRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
