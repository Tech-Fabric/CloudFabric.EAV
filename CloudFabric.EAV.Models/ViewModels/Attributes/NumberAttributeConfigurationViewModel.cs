using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class NumberAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public float DefaultValue { get; set; }
        public float? MinimumValue { get; set; }
        public float? MaximumValue { get; set; }
    }
}