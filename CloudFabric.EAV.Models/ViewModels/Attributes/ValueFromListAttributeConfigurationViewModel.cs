using CloudFabric.EAV.Domain.Models.Attributes;

namespace CloudFabric.EAV.Models.ViewModels.Attributes
{
    public class ValueFromListAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public List<ValueFromListOptionConfiguration> ValuesList { get; set; }
    }
}