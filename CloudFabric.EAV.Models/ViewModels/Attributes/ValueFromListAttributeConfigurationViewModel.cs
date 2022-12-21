using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Attributes;

namespace CloudFabric.EAV.Models.ViewModels.Attributes
{
    public class ValueFromListAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public ValueFromListAttributeType ValueFromListAttributeType { get; set; }
        public List<ValueFromListOptionConfiguration> ValuesList { get; set; }
        public string? AttributeMachineNameToAffect { get; set; }
    }
}