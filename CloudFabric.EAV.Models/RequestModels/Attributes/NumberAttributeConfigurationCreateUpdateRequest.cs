using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class NumberAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public float DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
        public NumberAttributeConfigurationValidationRequest Validators { get; set; }
    }

    public class NumberAttributeConfigurationValidationRequest: AttributeConfigurationValidationRequest
    {
        public float? MinimumValue { get; set; }
        public float? MaximumValue { get; set; }
    }
}