using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record AttributeInstanceRemoved : Event
{
    public AttributeInstanceRemoved()
    {
    }

    public AttributeInstanceRemoved(Guid entityInstanceId, Guid entityConfigurationId, string attributeMachineName)
    {
        AggregateId = entityInstanceId;
        EntityConfigurationId = entityConfigurationId;
        AttributeMachineName = attributeMachineName;
    }

    public Guid EntityConfigurationId { get; set; }

    public string AttributeMachineName { get; set; }
}

