using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category
{
    public record CategoryPathChanged(Guid entityConfigurationId, string currentCategoryPath, string newCategoryPath, Guid childConfigurationId): Event;
}