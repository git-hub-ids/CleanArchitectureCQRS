using MediatR;
using WorkflowManagementService.Application.Common.Interfaces;
using WorkflowManagementService.Application.DTOs;
using WorkflowManagementService.Application.Mappings;
using WorkflowManagementService.Domain.Common;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Application.Features.LookupItems.Queries.GetById;

public record GetLookupItemByIdQuery(Guid Id) : IRequest<Result<LookupItemDto>>;

public sealed class GetLookupItemByIdQueryHandler(
    ILookupItemRepository repository,
    ICacheService cache)
    : IRequestHandler<GetLookupItemByIdQuery, Result<LookupItemDto>>
{
    public async Task<Result<LookupItemDto>> Handle(GetLookupItemByIdQuery request, CancellationToken ct)
    {
        var cacheKey = $"lookup_items:{request.Id}";
        var cached = await cache.GetAsync<LookupItemDto>(cacheKey, ct);
        if (cached is not null)
            return Result<LookupItemDto>.Success(cached);

        var entity = await repository.GetByIdAsync(request.Id, ct);
        if (entity is null || entity.IsDeleted)
            return Result<LookupItemDto>.NotFound();

        var dto = entity.ToDto();
        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30), ct);
        return Result<LookupItemDto>.Success(dto);
    }
}

