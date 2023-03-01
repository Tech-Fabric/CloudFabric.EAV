using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class SerialAttributeConfigurationUpdateRequest : AttributeConfigurationCreateUpdateRequest
{
    public int Increment { get; set; }

    public override EavAttributeType ValueType { get; } = EavAttributeType.Serial;
}
