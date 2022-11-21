namespace CloudFabric.EAV.Models.ViewModels;

public class AttributeConfigurationListItemViewModel
{
    public List<LocalizedStringViewModel> Name { get; set; }

    public string MachineName { get; set; }

    public bool IsRequired { get; set; }
    
    public Guid? TenantId { get; set; }
}