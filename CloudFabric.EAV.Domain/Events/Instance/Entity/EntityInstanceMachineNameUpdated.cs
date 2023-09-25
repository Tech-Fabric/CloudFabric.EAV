using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record EntityInstanceMachineNameUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityInstanceMachineNameUpdated()
    {
    }

    public EntityInstanceMachineNameUpdated(Guid id, Guid entityConfigurationId, string newMachineName)
    {
        EntityConfigurationId = entityConfigurationId;
        AggregateId = id;
        NewMachineName = newMachineName;
    }

    public Guid EntityConfigurationId { get; set; }

    public string NewMachineName { get; set; }
}
