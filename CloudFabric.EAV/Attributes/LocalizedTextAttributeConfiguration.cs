using CloudFabric.EAV.Data.Enums;

using System.ComponentModel.DataAnnotations.Schema;

namespace CloudFabric.EAV.Attributes
{
    public class LocalizedTextAttributeConfiguration : AttributeConfiguration
    {
        
        public LocalizedString DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
    }
}
