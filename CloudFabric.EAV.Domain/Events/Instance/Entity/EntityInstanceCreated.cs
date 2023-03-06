using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record EntityInstanceCreated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityInstanceCreated()
    {
    }

    public EntityInstanceCreated(Guid id, Guid entityConfigurationId, List<AttributeInstance> attributes,
        Guid? tenantId)
    {
        TenantId = tenantId;
        Attributes = attributes;
        EntityConfigurationId = entityConfigurationId;
        AggregateId = id;
    }

    public Guid EntityConfigurationId { get; set; }
    public List<AttributeInstance> Attributes { get; set; }
    public Guid? TenantId { get; set; }
}
