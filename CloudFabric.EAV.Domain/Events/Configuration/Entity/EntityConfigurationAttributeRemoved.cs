using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationAttributeRemoved : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityConfigurationAttributeRemoved()
    {
    }

    public EntityConfigurationAttributeRemoved(Guid entityConfigurationId, Guid attributeConfigurationId)
    {
        AggregateId = entityConfigurationId;
        AttributeConfigurationId = attributeConfigurationId;
    }

    public Guid AttributeConfigurationId { get; set; }
}
