using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class NumberAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public float DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
    }
}