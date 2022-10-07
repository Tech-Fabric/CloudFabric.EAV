using CloudFabric.EAV.Data.Enums;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class NumberAttributeConfiguration : AttributeConfiguration
    {
        public float DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
    }
}