using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationDeleted : Event
{
    public AttributeConfigurationDeleted()
    {
    }

    public AttributeConfigurationDeleted(Guid id)
    {
        AggregateId = id;
    }
}