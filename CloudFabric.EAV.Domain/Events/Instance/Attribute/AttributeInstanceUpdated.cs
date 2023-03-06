using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Attribute;

public record AttributeInstanceUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
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
