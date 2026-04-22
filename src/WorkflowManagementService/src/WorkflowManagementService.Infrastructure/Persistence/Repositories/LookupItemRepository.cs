using Microsoft.EntityFrameworkCore;
using WorkflowManagementService.Domain.Entities;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Infrastructure.Persistence.Repositories;

public class LookupItemRepository(WorkflowDbContext context) : ILookupItemRepository
{
    public async Task<LookupItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.LookupItems.FindAsync([id], ct);

    public async Task<IReadOnlyList<LookupItem>> GetAllAsync(CancellationToken ct = default)
        => await context.LookupItems
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(ct);

    public async Task<bool> ExistsByCodeAsync(Guid categoryId, string code, Guid? excludeId = null, CancellationToken ct = default)
        => await context.LookupItems.AnyAsync(x =>
            x.CategoryId == categoryId &&
            x.Code == code.ToUpperInvariant() &&
            (excludeId == null || x.Id != excludeId), ct);

    public async Task<LookupItem?> GetWithCategoryAsync(Guid id, CancellationToken ct = default)
        => await context.LookupItems
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<LookupItem>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => await context.LookupItems
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(ct);

    public async Task AddAsync(LookupItem entity, CancellationToken ct = default)
        => await context.LookupItems.AddAsync(entity, ct);

    public void Update(LookupItem entity) => context.LookupItems.Update(entity);
    public void Remove(LookupItem entity) => context.LookupItems.Remove(entity);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}

