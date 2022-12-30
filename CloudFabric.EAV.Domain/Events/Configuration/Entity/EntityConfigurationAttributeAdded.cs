using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationAttributeAdded : Event
{
    public EntityConfigurationAttributeAdded()
    {
    }

    public EntityConfigurationAttributeAdded(Guid entityConfigurationId, EntityConfigurationAttributeReference attributeReference)
    {
        AggregateId = entityConfigurationId;
        AttributeReference = attributeReference;
    }

    public EntityConfigurationAttributeReference AttributeReference { get; set; }
}
