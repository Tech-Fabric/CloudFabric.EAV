using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record AttributeInstanceRemoved : Event
{
    public AttributeInstanceRemoved()
    {
    }

    public AttributeInstanceRemoved(Guid entityInstanceId, string attributeMachineName)
    {
        AggregateId = entityInstanceId;
        AttributeMachineName = attributeMachineName;
    }

    public string AttributeMachineName { get; set; }
}

