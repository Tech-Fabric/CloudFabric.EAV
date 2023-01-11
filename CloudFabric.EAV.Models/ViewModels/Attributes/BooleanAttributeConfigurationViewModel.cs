using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class BooleanAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public string TrueDisplayValue { get; set; }

        public string FalseDisplayValue { get; set; }
    }
}