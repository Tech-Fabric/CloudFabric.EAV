using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record DateRangeAttributeConfigurationUpdated(bool IsSingleDate) : Event;
}