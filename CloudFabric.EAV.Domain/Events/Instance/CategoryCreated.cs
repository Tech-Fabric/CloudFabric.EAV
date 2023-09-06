using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models;

public record CategoryCreated : Event
{
    public CategoryCreated()
    {

    }

    public CategoryCreated(Guid id,
        string machineName,
        Guid entityConfigurationId,
        List<AttributeInstance> attributes,
        Guid? tenantId)
    {
        TenantId = tenantId;
        Attributes = attributes;
        EntityConfigurationId = entityConfigurationId;
        AggregateId = id;
        MachineName = machineName;
    }

    public Guid EntityConfigurationId { get; set; }
    public List<AttributeInstance> Attributes { get; set; }
    public Guid? TenantId { get; set; }
    public string MachineName { get; set; }

}
