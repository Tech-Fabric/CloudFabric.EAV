using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class ArrayAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public EavAttributeType ItemsType { get; set; }

        public AttributeConfigurationViewModel ItemsAttributeConfiguration { get; set; }
    }
}