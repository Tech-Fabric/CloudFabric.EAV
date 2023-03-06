using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Attribute;

public record AttributeInstanceRemoved : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
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
