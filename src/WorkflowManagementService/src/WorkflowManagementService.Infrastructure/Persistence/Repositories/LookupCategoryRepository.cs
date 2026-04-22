using Microsoft.EntityFrameworkCore;
using WorkflowManagementService.Domain.Entities;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Infrastructure.Persistence.Repositories;

public class LookupCategoryRepository(WorkflowDbContext context) : ILookupCategoryRepository
{
    public async Task<LookupCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.LookupCategories.FindAsync([id], ct);

    public async Task<IReadOnlyList<LookupCategory>> GetAllAsync(CancellationToken ct = default)
        => await context.LookupCategories.OrderBy(x => x.DisplayOrder).ToListAsync(ct);

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
        => await context.LookupCategories.AnyAsync(x =>
            x.Code == code.ToUpperInvariant() && (excludeId == null || x.Id != excludeId), ct);

    public async Task<LookupCategory?> GetWithItemsAsync(Guid id, CancellationToken ct = default)
        => await context.LookupCategories
            .Include(x => x.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<LookupCategory>> GetActiveAsync(CancellationToken ct = default)
        => await context.LookupCategories
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(ct);

    public async Task AddAsync(LookupCategory entity, CancellationToken ct = default)
        => await context.LookupCategories.AddAsync(entity, ct);

    public void Update(LookupCategory entity) => context.LookupCategories.Update(entity);
    public void Remove(LookupCategory entity) => context.LookupCategories.Remove(entity);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}

