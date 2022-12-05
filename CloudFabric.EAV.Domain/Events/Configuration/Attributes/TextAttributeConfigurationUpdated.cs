using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record TextAttributeConfigurationUpdated(string DefaultValue) : Event;
}