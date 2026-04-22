using FluentValidation;
using MediatR;
using WorkflowManagementService.Application.Common.Interfaces;
using WorkflowManagementService.Application.DTOs;
using WorkflowManagementService.Application.Mappings;
using WorkflowManagementService.Domain.Common;
using WorkflowManagementService.Domain.Entities;
using WorkflowManagementService.Domain.Repositories;

namespace WorkflowManagementService.Application.Features.LookupItems.Commands.Create;

public record CreateLookupItemCommand(
    Guid CategoryId,
    string Code,
    string NameEn,
    string NameAr,
    string? Value,
    int DisplayOrder
) : IRequest<Result<LookupItemDto>>;

public sealed class CreateLookupItemCommandValidator : AbstractValidator<CreateLookupItemCommand>
{
    public CreateLookupItemCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z0-9_]+$")
            .WithMessage("Code must be uppercase alphanumeric with underscores");
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateLookupItemCommandHandler(
    ILookupItemRepository repository,
    ICurrentUserService currentUser,
    ICacheService cache,
    IPublisher publisher)
    : IRequestHandler<CreateLookupItemCommand, Result<LookupItemDto>>
{
    public async Task<Result<LookupItemDto>> Handle(CreateLookupItemCommand request, CancellationToken ct)
    {
        if (await repository.ExistsByCodeAsync(request.CategoryId, request.Code, ct: ct))
            return Result<LookupItemDto>.Failure($"Item with code '{request.Code}' already exists in category '{request.CategoryId}'.");

        var entity = LookupItem.Create(
            request.CategoryId,
            request.Code,
            request.NameEn,
            request.NameAr,
            request.Value,
            request.DisplayOrder,
            currentUser.UserId);

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        foreach (var domainEvent in entity.DomainEvents)
            await publisher.Publish(domainEvent, ct);
        entity.ClearDomainEvents();

        await cache.RemoveByPrefixAsync("lookup_items", ct);
        await cache.RemoveByPrefixAsync("lookup_categories", ct);

        return Result<LookupItemDto>.Success(entity.ToDto());
    }
}

