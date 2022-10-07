using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ArrayAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public EavAttributeType ItemsType { get; set; }

        public AttributeConfigurationViewModel ItemsAttributeConfiguration { get; set; }
    }
}