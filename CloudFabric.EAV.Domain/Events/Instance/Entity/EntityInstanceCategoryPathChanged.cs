using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity
{
    public record EntityInstanceCategoryPathChanged(Guid Id, string NewCategoryPath) : Event;
}