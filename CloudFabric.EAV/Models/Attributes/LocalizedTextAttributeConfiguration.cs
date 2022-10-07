using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Data.Models.Base;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class LocalizedTextAttributeConfiguration : AttributeConfiguration
    {

        public LocalizedString DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
    }
}
