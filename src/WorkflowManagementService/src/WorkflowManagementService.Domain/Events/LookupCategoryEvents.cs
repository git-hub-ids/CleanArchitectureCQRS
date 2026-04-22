using WorkflowManagementService.Domain.Common;

namespace WorkflowManagementService.Domain.Events;

public record LookupCategoryCreatedEvent(Guid CategoryId, string Code) : IDomainEvent;
public record LookupCategoryUpdatedEvent(Guid CategoryId, string Code) : IDomainEvent;
public record LookupCategoryDeletedEvent(Guid CategoryId, string Code) : IDomainEvent;

