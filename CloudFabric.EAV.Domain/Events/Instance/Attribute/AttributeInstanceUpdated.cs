using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record AttributeInstanceUpdated : Event
{
    public AttributeInstanceUpdated()
    {
    }

    public AttributeInstanceUpdated(Guid entityInstanceId, AttributeInstance attributeInstance)
    {
        AggregateId = entityInstanceId;
        AttributeInstance = attributeInstance;
    }

    public AttributeInstance AttributeInstance { get; set; }
}

