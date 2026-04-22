using MediatR;
using WorkflowManagementService.Application.Common.Interfaces;
using WorkflowManagementService.Application.DTOs;
using WorkflowManagementService.Application.Mappings;
using WorkflowManagementService.Domain.Common;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Application.Features.LookupCategories.Queries.GetById;

public record GetLookupCategoryByIdQuery(Guid Id) : IRequest<Result<LookupCategoryWithItemsDto>>;

public sealed class GetLookupCategoryByIdQueryHandler(
    ILookupCategoryRepository repository,
    ICacheService cache)
    : IRequestHandler<GetLookupCategoryByIdQuery, Result<LookupCategoryWithItemsDto>>
{
    public async Task<Result<LookupCategoryWithItemsDto>> Handle(GetLookupCategoryByIdQuery request, CancellationToken ct)
    {
        var cacheKey = $"lookup_categories:{request.Id}";
        var cached = await cache.GetAsync<LookupCategoryWithItemsDto>(cacheKey, ct);
        if (cached is not null)
            return Result<LookupCategoryWithItemsDto>.Success(cached);

        var entity = await repository.GetWithItemsAsync(request.Id, ct);
        if (entity is null || entity.IsDeleted)
            return Result<LookupCategoryWithItemsDto>.NotFound();

        var dto = entity.ToDtoWithItems();
        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30), ct);

        return Result<LookupCategoryWithItemsDto>.Success(dto);
    }
}

