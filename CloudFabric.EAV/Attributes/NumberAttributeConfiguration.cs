using CloudFabric.EAV.Data.Enums;

namespace CloudFabric.EAV.Attributes
{
    public class NumberAttributeConfiguration : AttributeConfiguration
    {
        public float DefaultValue { get; set; }
        
        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
    }
}