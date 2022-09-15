using CloudFabric.EAV.Data.Enums;

namespace CloudFabric.EAV.Attributes
{
    public class TextAttributeConfiguration : AttributeConfiguration
    {
        public string DefaultValue { get; set; }
        
        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
    }
}