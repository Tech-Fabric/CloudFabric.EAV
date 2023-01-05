using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category
{
    public record CategoryPathChanged(string newCategoryPath): Event;
}