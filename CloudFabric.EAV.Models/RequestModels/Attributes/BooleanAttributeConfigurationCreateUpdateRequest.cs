using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class BooleanAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public string TrueDisplayValue { get; set; } = "True";

        public string FalseDisplayValue { get; set; } = "False";

        public override EavAttributeType ValueType { get; } = EavAttributeType.Boolean;
    }
}