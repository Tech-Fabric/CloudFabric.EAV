using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationIsRequiredFlagUpdated : Event
{
    public AttributeConfigurationIsRequiredFlagUpdated()
    {
    }

    public AttributeConfigurationIsRequiredFlagUpdated(Guid id, bool newIsRequired)
    {
        AggregateId = id;
        NewIsRequired = newIsRequired;
    }

    public bool NewIsRequired { get; set; }
}
