using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ValueFromListConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {

        public override EavAttributeType ValueType => EavAttributeType.ValueFromList;

        public ValueFromListAttributeType ValueFromListAttributeType { get; }

        public List<ValueFromListOptionConfiguration> ValuesList { get; set; }

        public string? AttributeMachineNameToAffect { get; set; }
    }
}