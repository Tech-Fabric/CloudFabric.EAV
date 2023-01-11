using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category
{
    public record CategoryCreated(
        Guid Id,
        string PartitionKey,
        Guid EntityConfigurationId,
        ReadOnlyCollection<AttributeInstance> Attributes,
        Guid? TenantId,
        string CategoryPath,
        Guid ChildEntityConfigurationId,
        DateTime Timestamp) : Event;
}