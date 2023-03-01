using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class ArrayAttributeConfigurationViewModel : AttributeConfigurationViewModel
{
    public EavAttributeType ItemsType { get; set; }

    public AttributeConfigurationViewModel ItemsAttributeConfiguration { get; set; }
}
