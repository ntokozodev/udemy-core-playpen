using AuthPlaypen.Data.Data;
using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuthPlaypen.Application.Services;

public class ApplicationService(
    AuthPlaypenDbContext dbContext,
    IOpenIddictSyncOrchestrator<ApplicationDto> openIddictApplicationSyncService,
    IActorContextService actorContextService) : IApplicationService
{
    public async Task<CursorPagedResultDto<ApplicationDto>> GetPageAsync(Guid? cursor, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Applications
            .Include(a => a.ApplicationScopes)
            .ThenInclude(a => a.Scope)
            .AsNoTracking()
            .OrderBy(a => a.Id)
            .AsQueryable();

        if (cursor.HasValue)
        {
            query = query.Where(a => a.Id.CompareTo(cursor.Value) > 0);
        }

        var entities = await query.Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasMore = entities.Count > pageSize;
        var items = entities.Take(pageSize).Select(ToDto).ToList();
        var nextCursor = hasMore ? items.Last().Id.ToString() : null;
        return new CursorPagedResultDto<ApplicationDto>(items, nextCursor);
    }

    public async Task<ApplicationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications
            .Include(a => a.ApplicationScopes)
            .ThenInclude(a => a.Scope)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (application is null)
        {
            return null;
        }

        return ToDto(application);
    }


    public async Task<IReadOnlyCollection<ApplicationReferenceDto>> SearchAsync(string searchTerm, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedSearchTerm = searchTerm.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            return Array.Empty<ApplicationReferenceDto>();
        }

        return await dbContext.Applications
            .AsNoTracking()
            .Where(a => EF.Functions.ILike(a.DisplayName, $"%{normalizedSearchTerm}%") || EF.Functions.ILike(a.ClientId, $"%{normalizedSearchTerm}%"))
            .OrderBy(a => a.DisplayName)
            .Take(pageSize)
            .Select(a => new ApplicationReferenceDto(a.Id, a.DisplayName, a.ClientId))
            .ToListAsync(cancellationToken);
    }

    public async Task<(ApplicationDto? Application, string? Error)> CreateAsync(CreateApplicationRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.Applications.AnyAsync(a => a.ClientId == request.ClientId, cancellationToken))
        {
            return (null, "An application with this ClientId already exists.");
        }

        var redirectUrisValidationError = ValidateRedirectUris(request.Flow, request.RedirectUris, request.PostLogoutRedirectUris);
        if (redirectUrisValidationError is not null)
        {
            return (null, redirectUrisValidationError);
        }

        var actor = actorContextService.GetCurrentActor();
        var actorDescriptor = BuildActorDescriptor(actor);
        var now = DateTimeOffset.UtcNow;

        var application = new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName,
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
            Flow = request.Flow,
            PostLogoutRedirectUris = request.PostLogoutRedirectUris,
            RedirectUris = request.RedirectUris,
            CreatedBy = actorDescriptor,
            UpdatedBy = actorDescriptor,
            CreatedAt = now,
            UpdatedAt = now
        };

        var scopeIds = request.ScopeIds?.Distinct().ToList() ?? [];

        var scopes = await dbContext.Scopes
            .Include(s => s.ApplicationScopes)
            .Where(s => scopeIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (scopes.Count != scopeIds.Count)
        {
            return (null, "One or more scope IDs are invalid.");
        }

        var scopeValidationError = ValidateScopeAssignments(scopes, application.Id);
        if (scopeValidationError is not null)
        {
            return (null, scopeValidationError);
        }

        foreach (var scope in scopes)
        {
            application.ApplicationScopes.Add(new ApplicationScopeEntity
            {
                ApplicationId = application.Id,
                ScopeId = scope.Id
            });
        }

        dbContext.Applications.Add(application);
        dbContext.EntityAuditHistory.Add(CreateAuditEntry("application", application.Id, "created", actor, now, new
        {
            application.DisplayName,
            application.ClientId,
            application.Flow,
            ScopeIds = scopeIds
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = ToDto(application);
        await openIddictApplicationSyncService.HandleCreationAsync(dto, cancellationToken);

        return (dto, null);
    }

    public async Task<(ApplicationDto? Application, string? Error, bool NotFound)> UpdateAsync(Guid id, UpdateApplicationRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.Applications.AnyAsync(a => a.Id != id && a.ClientId == request.ClientId, cancellationToken))
        {
            return (null, "An application with this ClientId already exists.", false);
        }

        var redirectUrisValidationError = ValidateRedirectUris(request.Flow, request.RedirectUris, request.PostLogoutRedirectUris);
        if (redirectUrisValidationError is not null)
        {
            return (null, redirectUrisValidationError, false);
        }

        var application = await dbContext.Applications
            .Include(a => a.ApplicationScopes)
            .ThenInclude(a => a.Scope)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (application is null)
        {
            return (null, null, true);
        }

        var scopeIds = request.ScopeIds?.Distinct().ToList() ?? [];

        var scopes = await dbContext.Scopes
            .Include(s => s.ApplicationScopes)
            .Where(s => scopeIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        if (scopes.Count != scopeIds.Count)
        {
            return (null, "One or more scope IDs are invalid.", false);
        }

        var scopeValidationError = ValidateScopeAssignments(scopes, application.Id);
        if (scopeValidationError is not null)
        {
            return (null, scopeValidationError, false);
        }

        var actor = actorContextService.GetCurrentActor();
        var now = DateTimeOffset.UtcNow;

        application.DisplayName = request.DisplayName;
        application.ClientId = request.ClientId;
        application.ClientSecret = request.ClientSecret;
        application.Flow = request.Flow;
        application.PostLogoutRedirectUris = request.PostLogoutRedirectUris;
        application.RedirectUris = request.RedirectUris;
        application.UpdatedBy = BuildActorDescriptor(actor);
        application.UpdatedAt = now;

        application.ApplicationScopes.Clear();
        foreach (var scope in scopes)
        {
            application.ApplicationScopes.Add(new ApplicationScopeEntity
            {
                ApplicationId = application.Id,
                ScopeId = scope.Id
            });
        }

        dbContext.EntityAuditHistory.Add(CreateAuditEntry("application", application.Id, "updated", actor, now, new
        {
            application.DisplayName,
            application.ClientId,
            application.Flow,
            ScopeIds = scopeIds
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = ToDto(application);
        await openIddictApplicationSyncService.HandleUpdateAsync(dto, cancellationToken);

        return (dto, null, false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications
            .Include(a => a.ApplicationScopes)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (application is null)
        {
            return false;
        }

        var actor = actorContextService.GetCurrentActor();
        var now = DateTimeOffset.UtcNow;

        if (application.ApplicationScopes.Count > 0)
        {
            dbContext.ApplicationScopes.RemoveRange(application.ApplicationScopes);
        }

        dbContext.EntityAuditHistory.Add(CreateAuditEntry("application", application.Id, "deleted", actor, now, new
        {
            application.DisplayName,
            application.ClientId
        }));
        dbContext.Applications.Remove(application);
        await dbContext.SaveChangesAsync(cancellationToken);
        await openIddictApplicationSyncService.HandleDeletionAsync(id, cancellationToken);
        return true;
    }

    private static ApplicationDto ToDto(ApplicationEntity application)
    {
        var scopeDtos = application.ApplicationScopes
            .Select(x => x.Scope)
            .Select(s => new ScopeReferenceDto(s.Id, s.DisplayName, s.ScopeName, s.Description))
            .ToList();

        return new ApplicationDto(
            application.Id,
            application.DisplayName,
            application.ClientId,
            application.ClientSecret,
            application.Flow,
            application.PostLogoutRedirectUris,
            application.RedirectUris,
            scopeDtos,
            new EntityMetadataDto(application.CreatedBy, application.CreatedAt, application.UpdatedBy, application.UpdatedAt));
    }

    private static string? ValidateRedirectUris(ApplicationFlow flow, string? redirectUris, string? postLogoutRedirectUris)
    {
        if (flow == ApplicationFlow.AuthorizationWithPKCE)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(redirectUris) || !string.IsNullOrWhiteSpace(postLogoutRedirectUris))
        {
            return "RedirectUris and PostLogoutRedirectUris are only allowed for AuthorizationWithPKCE flow.";
        }

        return null;
    }

    private static string? ValidateScopeAssignments(IEnumerable<ScopeEntity> scopes, Guid applicationId)
    {
        var hasDisallowedScope = scopes.Any(scope =>
            !scope.IsGlobal &&
            !scope.ApplicationScopes.Any(applicationScope => applicationScope.ApplicationId == applicationId));

        if (hasDisallowedScope)
        {
            return "One or more scopes are not allowed for this application.";
        }

        return null;
    }

    private static EntityAuditHistoryEntry CreateAuditEntry(string entityType, Guid entityId, string action, ActorContext actor, DateTimeOffset occurredAt, object summary)
    {
        return new EntityAuditHistoryEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorDisplayName = actor.DisplayName,
            ActorEmail = actor.Email,
            OccurredAt = occurredAt,
            ChangeSummaryJson = JsonSerializer.Serialize(summary)
        };
    }

    private static string BuildActorDescriptor(ActorContext actor)
    {
        return string.IsNullOrWhiteSpace(actor.Email)
            ? actor.DisplayName
            : $"{actor.DisplayName} <{actor.Email}>";
    }
}
