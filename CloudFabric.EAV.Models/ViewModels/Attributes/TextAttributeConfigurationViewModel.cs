using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class TextAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public string DefaultValue { get; set; }
    }
}