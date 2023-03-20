using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class ValueFromListAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
{
    public override EavAttributeType ValueType => EavAttributeType.ValueFromList;

    public List<ValueFromListOptionCreateUpdateRequest> ValuesList { get; set; }
}
