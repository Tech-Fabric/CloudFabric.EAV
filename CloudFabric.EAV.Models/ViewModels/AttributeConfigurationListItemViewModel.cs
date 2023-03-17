using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models.ViewModels;

public class AttributeConfigurationListItemViewModel
{
    public Guid? Id { get; set; }

    public List<LocalizedStringViewModel> Name { get; set; }

    public List<LocalizedStringViewModel> Description { get; set; }

    public string MachineName { get; set; }

    public bool IsRequired { get; set; }

    public Guid? TenantId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public EavAttributeType ValueType { get; set; }

    public int NumberOfEntityInstancesWithAttribute { get; set; }

    public string? Metadata { get; set; }
}
