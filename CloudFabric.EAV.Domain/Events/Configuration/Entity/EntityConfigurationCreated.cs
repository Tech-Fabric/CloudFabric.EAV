using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationCreated : Event
{
    public EntityConfigurationCreated()
    {
    }

    public EntityConfigurationCreated(Guid id, List<LocalizedString> name, string machineName,
        List<EntityConfigurationAttributeReference> attributes, Guid? tenantId)
    {
        AggregateId = id;
        TenantId = tenantId;
        Attributes = attributes;
        MachineName = machineName;
        Name = name;
    }

    public List<LocalizedString> Name { get; set; }

    public string MachineName { get; set; }

    public List<EntityConfigurationAttributeReference> Attributes { get; set; }

    public Guid? TenantId { get; set; }
}
