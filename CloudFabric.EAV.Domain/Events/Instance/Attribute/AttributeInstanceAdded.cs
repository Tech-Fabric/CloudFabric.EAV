using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Attribute;

public record AttributeInstanceAdded : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public AttributeInstanceAdded()
    {
    }

    public AttributeInstanceAdded(Guid entityInstanceId, AttributeInstance attributeInstance)
    {
        AggregateId = entityInstanceId;
        AttributeInstance = attributeInstance;
    }

    public AttributeInstance AttributeInstance { get; set; }
}
