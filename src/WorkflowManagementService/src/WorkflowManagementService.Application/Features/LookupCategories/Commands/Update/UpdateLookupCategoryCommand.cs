using MediatR;
using WorkflowManagementService.Application.Common.Interfaces;
using WorkflowManagementService.Application.DTOs;
using WorkflowManagementService.Application.Mappings;
using WorkflowManagementService.Domain.Common;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Application.Features.LookupCategories.Commands.Update;

public record UpdateLookupCategoryCommand(
    Guid Id,
    string NameEn,
    string NameAr,
    string? Description,
    int DisplayOrder,
    bool IsActive
) : IRequest<Result<LookupCategoryDto>>;

public sealed class UpdateLookupCategoryCommandHandler(
    ILookupCategoryRepository repository,
    ICurrentUserService currentUser,
    ICacheService cache,
    IPublisher publisher)
    : IRequestHandler<UpdateLookupCategoryCommand, Result<LookupCategoryDto>>
{
    public async Task<Result<LookupCategoryDto>> Handle(UpdateLookupCategoryCommand request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(request.Id, ct);
        if (entity is null || entity.IsDeleted)
            return Result<LookupCategoryDto>.NotFound($"LookupCategory {request.Id} not found.");

        entity.Update(
            request.NameEn,
            request.NameAr,
            request.Description,
            request.DisplayOrder,
            request.IsActive,
            currentUser.UserId);

        repository.Update(entity);
        await repository.SaveChangesAsync(ct);

        foreach (var e in entity.DomainEvents)
            await publisher.Publish(e, ct);
        entity.ClearDomainEvents();

        await cache.RemoveByPrefixAsync("lookup_categories", ct);

        return Result<LookupCategoryDto>.Success(entity.ToDto());
    }
}

