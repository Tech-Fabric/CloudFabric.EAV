using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record DateRangeAttributeConfigurationUpdated(DateRangeAttributeType DateRangeAttributeType) : Event;
}