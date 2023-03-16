using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public abstract class AttributeConfigurationViewModel
{
    public Guid Id { get; set; }

    public bool IsRequired { get; set; }

    public List<LocalizedStringViewModel> Name { get; set; }

    public List<LocalizedStringViewModel> Description { get; set; }

    public string MachineName { get; set; }

    public EavAttributeType ValueType { get; set; }

    public Guid? TenantId { get; set; }

    public string? Metadata { get; set; }
}
