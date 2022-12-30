using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationAttributeUpdated : Event
{
    public EntityConfigurationAttributeUpdated()
    {
    }

    public EntityConfigurationAttributeUpdated(Guid entityConfigurationId, AttributeConfiguration attribute)
    {
        AggregateId = entityConfigurationId;
        Attribute = attribute;
    }

    public AttributeConfiguration Attribute { get; set; }
}

