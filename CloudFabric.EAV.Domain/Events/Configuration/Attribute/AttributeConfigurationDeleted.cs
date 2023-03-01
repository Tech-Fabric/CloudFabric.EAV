using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationDeleted : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public AttributeConfigurationDeleted()
    {
    }

    public AttributeConfigurationDeleted(Guid id)
    {
        AggregateId = id;
    }
}
