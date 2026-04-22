using WorkflowManagementService.Domain.Common;

namespace WorkflowManagementService.Domain.Events;

public record LookupItemCreatedEvent(Guid ItemId, Guid CategoryId, string Code) : IDomainEvent;
public record LookupItemUpdatedEvent(Guid ItemId, Guid CategoryId, string Code) : IDomainEvent;
public record LookupItemDeletedEvent(Guid ItemId, Guid CategoryId, string Code) : IDomainEvent;

