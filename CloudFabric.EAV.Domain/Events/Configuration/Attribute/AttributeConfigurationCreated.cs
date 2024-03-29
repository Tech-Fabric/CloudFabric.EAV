using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationCreated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public AttributeConfigurationCreated()
    {
    }

    public AttributeConfigurationCreated(Guid id, string machineName, List<LocalizedString> name,
        EavAttributeType valueType, List<LocalizedString>? description, bool isRequired, Guid? tenantId,
        string? metadata)
    {
        AggregateId = id;
        MachineName = machineName;
        Name = name;
        ValueType = valueType;
        Description = description;
        IsRequired = isRequired;
        TenantId = tenantId;
        Metadata = metadata;
    }

    public string MachineName { get; set; }

    public List<LocalizedString> Name { get; set; }

    public EavAttributeType ValueType { get; set; }

    public List<LocalizedString>? Description { get; set; }

    public bool IsRequired { get; set; }

    public Guid? TenantId { get; set; }

    public string? Metadata { get; set; }
}
