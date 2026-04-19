using AuthPlaypen.Data.Data;
using AuthPlaypen.Application.Dtos;
using AuthPlaypen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuthPlaypen.Application.Services;

public class ScopeService(
    AuthPlaypenDbContext dbContext,
    IOpenIddictSyncOrchestrator<ScopeDto> openIddictScopeSyncService,
    IActorContextService actorContextService) : IScopeService
{
    public async Task<CursorPagedResultDto<ScopeDto>> GetPageAsync(Guid? cursor, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Scopes
            .Include(s => s.ApplicationScopes)
            .ThenInclude(x => x.Application)
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .AsQueryable();

        if (cursor.HasValue)
        {
            query = query.Where(s => s.Id.CompareTo(cursor.Value) > 0);
        }

        var entities = await query.Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasMore = entities.Count > pageSize;
        var items = entities.Take(pageSize).Select(ToDto).ToList();
        var nextCursor = hasMore ? items.Last().Id.ToString() : null;
        return new CursorPagedResultDto<ScopeDto>(items, nextCursor);
    }

    public async Task<ScopeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var scope = await dbContext.Scopes
            .Include(s => s.ApplicationScopes)
            .ThenInclude(x => x.Application)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return scope is null ? null : ToDto(scope);
    }


    public async Task<IReadOnlyCollection<ScopeReferenceDto>> SearchAsync(string searchTerm, int pageSize, CancellationToken cancellationToken)
    {
        var normalizedSearchTerm = searchTerm.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            return Array.Empty<ScopeReferenceDto>();
        }

        return await dbContext.Scopes
            .AsNoTracking()
            .Where(s => EF.Functions.ILike(s.DisplayName, $"%{normalizedSearchTerm}%") || EF.Functions.ILike(s.ScopeName, $"%{normalizedSearchTerm}%"))
            .OrderBy(s => s.DisplayName)
            .Take(pageSize)
            .Select(s => new ScopeReferenceDto(s.Id, s.DisplayName, s.ScopeName, s.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<(ScopeDto? Scope, string? Error)> CreateAsync(CreateScopeRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.Scopes.AnyAsync(s => s.ScopeName == request.ScopeName, cancellationToken))
        {
            return (null, "A scope with this ScopeName already exists.");
        }

        var appIds = request.ApplicationIds?.Distinct().ToList() ?? [];
        if (appIds.Count > 0)
        {
            var appCount = await dbContext.Applications.CountAsync(a => appIds.Contains(a.Id), cancellationToken);
            if (appCount != appIds.Count)
            {
                return (null, "One or more application IDs are invalid.");
            }
        }

        var actor = actorContextService.GetCurrentActor();
        var actorDescriptor = BuildActorDescriptor(actor);
        var now = DateTimeOffset.UtcNow;

        var scope = new ScopeEntity
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName,
            ScopeName = request.ScopeName,
            Description = request.Description,
            CreatedBy = actorDescriptor,
            UpdatedBy = actorDescriptor,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var appId in appIds)
        {
            scope.ApplicationScopes.Add(new ApplicationScopeEntity
            {
                ApplicationId = appId,
                ScopeId = scope.Id
            });
        }

        dbContext.Scopes.Add(scope);
        dbContext.EntityAuditHistory.Add(CreateAuditEntry("scope", scope.Id, "created", actor, now, new
        {
            scope.DisplayName,
            scope.ScopeName,
            scope.Description,
            ApplicationIds = appIds
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await dbContext.Scopes
            .Include(s => s.ApplicationScopes)
            .ThenInclude(x => x.Application)
            .AsNoTracking()
            .FirstAsync(s => s.Id == scope.Id, cancellationToken);

        var dto = ToDto(reloaded);
        await openIddictScopeSyncService.HandleCreationAsync(dto, cancellationToken);

        return (dto, null);
    }

    public async Task<(ScopeDto? Scope, string? Error, bool NotFound)> UpdateAsync(Guid id, UpdateScopeRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.Scopes.AnyAsync(s => s.Id != id && s.ScopeName == request.ScopeName, cancellationToken))
        {
            return (null, "A scope with this ScopeName already exists.", false);
        }

        var scope = await dbContext.Scopes
            .Include(s => s.ApplicationScopes)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (scope is null)
        {
            return (null, null, true);
        }

        var appIds = request.ApplicationIds?.Distinct().ToHashSet() ?? [];
        if (appIds.Count > 0)
        {
            var appCount = await dbContext.Applications.CountAsync(a => appIds.Contains(a.Id), cancellationToken);
            if (appCount != appIds.Count)
            {
                return (null, "One or more application IDs are invalid.", false);
            }
        }

        var actor = actorContextService.GetCurrentActor();
        var now = DateTimeOffset.UtcNow;

        scope.DisplayName = request.DisplayName;
        scope.ScopeName = request.ScopeName;
        scope.Description = request.Description;
        scope.UpdatedBy = BuildActorDescriptor(actor);
        scope.UpdatedAt = now;
        scope.ApplicationScopes.Clear();
        foreach (var appId in appIds)
        {
            scope.ApplicationScopes.Add(new ApplicationScopeEntity
            {
                ApplicationId = appId,
                ScopeId = scope.Id
            });
        }

        dbContext.EntityAuditHistory.Add(CreateAuditEntry("scope", scope.Id, "updated", actor, now, new
        {
            scope.DisplayName,
            scope.ScopeName,
            scope.Description,
            ApplicationIds = appIds
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await dbContext.Scopes
            .Include(s => s.ApplicationScopes)
            .ThenInclude(x => x.Application)
            .AsNoTracking()
            .FirstAsync(s => s.Id == id, cancellationToken);

        var dto = ToDto(reloaded);
        await openIddictScopeSyncService.HandleUpdateAsync(dto, cancellationToken);

        return (dto, null, false);
    }

    public async Task<(bool Deleted, string? Error, bool NotFound)> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var scope = await dbContext.Scopes
            .Include(s => s.ApplicationScopes)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (scope is null)
        {
            return (false, null, true);
        }

        var actor = actorContextService.GetCurrentActor();
        var now = DateTimeOffset.UtcNow;

        if (scope.ApplicationScopes.Count > 0)
        {
            dbContext.ApplicationScopes.RemoveRange(scope.ApplicationScopes);
        }

        dbContext.EntityAuditHistory.Add(CreateAuditEntry("scope", scope.Id, "deleted", actor, now, new
        {
            scope.DisplayName,
            scope.ScopeName
        }));
        dbContext.Scopes.Remove(scope);
        await dbContext.SaveChangesAsync(cancellationToken);
        await openIddictScopeSyncService.HandleDeletionAsync(id, cancellationToken);
        return (true, null, false);
    }

    private static ScopeDto ToDto(ScopeEntity scope)
    {
        var applications = scope.ApplicationScopes.Count == 0
            ? Array.Empty<ApplicationReferenceDto>()
            : scope.ApplicationScopes
                .Select(x => new ApplicationReferenceDto(
                    x.Application.Id,
                    x.Application.DisplayName,
                    x.Application.ClientId))
                .ToArray();

        return new ScopeDto(
            scope.Id,
            scope.DisplayName,
            scope.ScopeName,
            scope.Description,
            applications,
            new EntityMetadataDto(scope.CreatedBy, scope.CreatedAt, scope.UpdatedBy, scope.UpdatedAt));
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
