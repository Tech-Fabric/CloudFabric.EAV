using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class LocalizedTextAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
{
    public string DefaultValue { get; set; }

    public override EavAttributeType ValueType { get; } = EavAttributeType.LocalizedText;
}
