using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record ValueFromListConfigurationUpdated(ValueFromListAttributeType ValueFromListAttributeType, List<ValueFromListOptionConfiguration> ValueFromListOptions) : Event;
}