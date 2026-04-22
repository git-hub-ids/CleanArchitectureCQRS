using MediatR;
using WorkflowManagementService.Application.Common.Interfaces;
using WorkflowManagementService.Domain.Common;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Application.Features.LookupItems.Commands.Delete;

public record DeleteLookupItemCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteLookupItemCommandHandler(
    ILookupItemRepository repository,
    ICurrentUserService currentUser,
    ICacheService cache,
    IPublisher publisher)
    : IRequestHandler<DeleteLookupItemCommand, Result>
{
    public async Task<Result> Handle(DeleteLookupItemCommand request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(request.Id, ct);
        if (entity is null || entity.IsDeleted)
            return Result.NotFound($"LookupItem {request.Id} not found.");

        entity.Delete(currentUser.UserId);

        repository.Update(entity);
        await repository.SaveChangesAsync(ct);

        foreach (var e in entity.DomainEvents)
            await publisher.Publish(e, ct);
        entity.ClearDomainEvents();

        await cache.RemoveByPrefixAsync("lookup_items", ct);
        await cache.RemoveByPrefixAsync("lookup_categories", ct);

        return Result.Success();
    }
}

