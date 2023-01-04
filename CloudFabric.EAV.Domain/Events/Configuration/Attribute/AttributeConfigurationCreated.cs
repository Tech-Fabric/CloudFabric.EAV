using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationCreated : Event
{
    public AttributeConfigurationCreated()
    {
    }

    public AttributeConfigurationCreated(Guid id, string machineName, List<LocalizedString> name, EavAttributeType valueType, List<LocalizedString> description, bool isRequired, Guid? tenantId)
    {
        AggregateId = id;
        MachineName = machineName;
        Name = name;
        ValueType = valueType;
        Description = description;
        IsRequired = isRequired;
        TenantId = tenantId;
    }

    public string MachineName { get; set;}

    public List<LocalizedString> Name { get; set; }

    public EavAttributeType ValueType { get; set; }

    public List<LocalizedString> Description { get; set; }

    public bool IsRequired { get; set; }

    public Guid? TenantId { get; set; }
}