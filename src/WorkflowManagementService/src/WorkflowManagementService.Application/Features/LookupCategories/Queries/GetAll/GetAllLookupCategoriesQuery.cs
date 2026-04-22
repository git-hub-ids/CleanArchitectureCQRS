using MediatR;
using WorkflowManagementService.Application.Common.Interfaces;
using WorkflowManagementService.Application.DTOs;
using WorkflowManagementService.Application.Mappings;
using WorkflowManagementService.Domain.Common;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Application.Features.LookupCategories.Queries.GetAll;

public record GetAllLookupCategoriesQuery : IRequest<Result<IReadOnlyList<LookupCategoryDto>>>;

public sealed class GetAllLookupCategoriesQueryHandler(
    ILookupCategoryRepository repository,
    ICacheService cache)
    : IRequestHandler<GetAllLookupCategoriesQuery, Result<IReadOnlyList<LookupCategoryDto>>>
{
    private const string CacheKey = "lookup_categories:all";

    public async Task<Result<IReadOnlyList<LookupCategoryDto>>> Handle(GetAllLookupCategoriesQuery request, CancellationToken ct)
    {
        var cached = await cache.GetAsync<IReadOnlyList<LookupCategoryDto>>(CacheKey, ct);
        if (cached is not null)
            return Result<IReadOnlyList<LookupCategoryDto>>.Success(cached);

        var entities = await repository.GetAllAsync(ct);
        var dtos = entities
            .Where(e => !e.IsDeleted)
            .Select(e => e.ToDto())
            .ToList()
            .AsReadOnly() as IReadOnlyList<LookupCategoryDto>;

        await cache.SetAsync(CacheKey, dtos!, TimeSpan.FromMinutes(30), ct);
        return Result<IReadOnlyList<LookupCategoryDto>>.Success(dtos!);
    }
}

