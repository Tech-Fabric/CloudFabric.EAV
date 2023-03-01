using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class HtmlTextAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
{
    public string DefaultValue { get; set; }

    public List<string> AllowedTags { get; set; }

    public override EavAttributeType ValueType { get; } = EavAttributeType.HtmlText;
}
