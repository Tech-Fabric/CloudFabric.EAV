using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record LocalizedTextAttributeConfigurationUpdated : Event
{
    public LocalizedTextAttributeConfigurationUpdated()
    {
    }

    public LocalizedTextAttributeConfigurationUpdated(Guid id, LocalizedString defaultValue)
    {
        AggregateId = id;
        DefaultValue = defaultValue;
    }

    public LocalizedString DefaultValue { get; set; }
}
