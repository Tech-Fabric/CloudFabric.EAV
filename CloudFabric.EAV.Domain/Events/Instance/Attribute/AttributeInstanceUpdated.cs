using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record AttributeInstanceUpdated : Event
{
    public AttributeInstanceUpdated()
    {
    }

    public AttributeInstanceUpdated(
        Guid entityInstanceId,
        Guid entityConfigurationId,
        AttributeInstance attributeInstance
    )
    {
        AggregateId = entityInstanceId;
        EntityConfigurationId = entityConfigurationId;
        AttributeInstance = attributeInstance;
    }

    public Guid EntityConfigurationId { get; set; }

    public AttributeInstance AttributeInstance { get; set; }
}