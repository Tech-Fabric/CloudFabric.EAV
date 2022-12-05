using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class TextAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public string DefaultValue { get; set; }
        
        public bool IsSearchable { get; set; }
        
        public int? MaxLength { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
    }
}