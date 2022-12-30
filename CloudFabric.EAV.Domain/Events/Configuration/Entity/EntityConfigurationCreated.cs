using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationCreated : Event
{
    public EntityConfigurationCreated()
    {
    }

    public EntityConfigurationCreated(Guid id, List<LocalizedString> name, string machineName, List<EntityConfigurationAttributeReference> attributes, Guid? tenantId, Dictionary<string, object> metadata)
    {
        AggregateId = id;
        Metadata = metadata;
        TenantId = tenantId;
        Attributes = attributes;
        MachineName = machineName;
        Name = name;
    }

    public List<LocalizedString> Name { get; set; }

    public string MachineName { get; set; }

    public List<EntityConfigurationAttributeReference> Attributes { get; set; }

    public Guid? TenantId { get; set; }

    public Dictionary<string, object> Metadata { get; set; }
}

