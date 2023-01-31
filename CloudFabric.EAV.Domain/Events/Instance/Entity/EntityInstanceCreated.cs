using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record EntityInstanceCreated : Event
{

    public EntityInstanceCreated(Guid id, Guid entityConfigurationId, List<AttributeInstance> attributes, Guid? tenantId, string categoryPath)
    {
        TenantId = tenantId;
        Attributes = attributes;
        EntityConfigurationId = entityConfigurationId;
        AggregateId = id;
        CategoryPath = categoryPath;
    }

    public Guid EntityConfigurationId { get; set; }
    public List<AttributeInstance> Attributes { get; set; }
    public Guid? TenantId { get; set; }
    public string CategoryPath { get; set; }
}
