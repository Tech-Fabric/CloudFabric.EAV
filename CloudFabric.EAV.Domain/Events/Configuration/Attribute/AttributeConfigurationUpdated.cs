using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationUpdated : Event
{
    public AttributeConfigurationUpdated()
    {
    }

    public AttributeConfigurationUpdated(Guid id, List<LocalizedString> name, List<LocalizedString> description,
        bool isRequired, Guid? tenantId, string? metadata)
    {
        AggregateId = id;
        Name = name;
        Description = description;
        IsRequired = isRequired;
        TenantId = tenantId;
        Metadata = metadata;
    }

    public List<LocalizedString> Name { get; set; }

    public List<LocalizedString> Description { get; set; }

    public bool IsRequired { get; set; }

    public Guid? TenantId { get; set; }

    public string? Metadata { get; set; }
}
