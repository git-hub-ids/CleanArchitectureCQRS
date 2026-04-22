using MediatR;
using WorkflowManagementService.Application.Common.Interfaces;
using WorkflowManagementService.Application.DTOs;
using WorkflowManagementService.Application.Mappings;
using WorkflowManagementService.Domain.Common;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Application.Features.LookupItems.Commands.Update;

public record UpdateLookupItemCommand(
    Guid Id,
    string NameEn,
    string NameAr,
    string? Value,
    int DisplayOrder,
    bool IsActive
) : IRequest<Result<LookupItemDto>>;

public sealed class UpdateLookupItemCommandHandler(
    ILookupItemRepository repository,
    ICurrentUserService currentUser,
    ICacheService cache,
    IPublisher publisher)
    : IRequestHandler<UpdateLookupItemCommand, Result<LookupItemDto>>
{
    public async Task<Result<LookupItemDto>> Handle(UpdateLookupItemCommand request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(request.Id, ct);
        if (entity is null || entity.IsDeleted)
            return Result<LookupItemDto>.NotFound($"LookupItem {request.Id} not found.");

        entity.Update(
            request.NameEn,
            request.NameAr,
            request.Value,
            request.DisplayOrder,
            request.IsActive,
            currentUser.UserId);

        repository.Update(entity);
        await repository.SaveChangesAsync(ct);

        foreach (var e in entity.DomainEvents)
            await publisher.Publish(e, ct);
        entity.ClearDomainEvents();

        await cache.RemoveByPrefixAsync("lookup_items", ct);
        await cache.RemoveByPrefixAsync("lookup_categories", ct);

        return Result<LookupItemDto>.Success(entity.ToDto());
    }
}

