using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class NumberAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
{
    public decimal DefaultValue { get; set; }
    public decimal? MinimumValue { get; set; }
    public decimal? MaximumValue { get; set; }

    public NumberAttributeType NumberType { get; set; } = NumberAttributeType.Integer;

    public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
}
