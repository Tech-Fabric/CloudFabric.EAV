using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class ArrayAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
{
    public override EavAttributeType ValueType { get; } = EavAttributeType.Array;

    public EavAttributeType ItemsType { get; set; }

    public AttributeConfigurationCreateUpdateRequest? ItemsAttributeConfiguration { get; set; }
}
