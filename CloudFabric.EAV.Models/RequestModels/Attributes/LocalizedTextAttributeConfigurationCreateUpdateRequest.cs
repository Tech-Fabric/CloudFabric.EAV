using CloudFabric.EAV.Data.Enums;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class LocalizedTextAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public string DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.LocalizedText;
    }
}