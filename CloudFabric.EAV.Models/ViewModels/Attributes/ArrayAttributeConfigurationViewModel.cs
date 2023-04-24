using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class ArrayAttributeConfigurationViewModel : AttributeConfigurationViewModel
{
    public EavAttributeType ItemsType { get; set; }

    public Guid ItemsAttributeConfigurationId { get; set; }
}
