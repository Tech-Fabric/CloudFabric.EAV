using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record AttributeInstanceUpdated : Event
{
    public AttributeInstanceUpdated()
    {
    }

    public AttributeInstanceUpdated(Guid entityInstanceId,
        AttributeInstance attributeInstance,
        Guid? entityConfigurationIdToReIndex,
        string categoryPath)
    {
        AggregateId = entityInstanceId;
        AttributeInstance = attributeInstance;
        EntityConfigurationIdToReIndex = entityConfigurationIdToReIndex;
        CategoryPath = categoryPath;
    }

    public AttributeInstance AttributeInstance { get; set; }
    public Guid? EntityConfigurationIdToReIndex { get; set; }
    public string CategoryPath { get; set; }
}

