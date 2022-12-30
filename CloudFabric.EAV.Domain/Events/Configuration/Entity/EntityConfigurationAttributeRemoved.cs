using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationAttributeRemoved : Event
{
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
