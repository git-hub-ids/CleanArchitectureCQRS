using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WorkflowManagementService.Domain.Entities;

namespace WorkflowManagementService.Infrastructure.Persistence;

public class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<LookupCategory> LookupCategories => Set<LookupCategory>();
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.HasDefaultSchema("workflow");

        builder.Entity<LookupCategory>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<LookupItem>().HasQueryFilter(e => !e.IsDeleted);
    }
}

