using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ValueFromListAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {

        public override EavAttributeType ValueType => EavAttributeType.ValueFromList;

        public ValueFromListAttributeType ValueFromListAttributeType { get; set; }

        public List<ValueFromListOptionCreateUpdateRequest> ValuesList { get; set; }

        public string? AttributeMachineNameToAffect { get; set; }
    }
}