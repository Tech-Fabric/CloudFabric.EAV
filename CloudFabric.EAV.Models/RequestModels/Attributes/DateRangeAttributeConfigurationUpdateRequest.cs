using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class DateRangeAttributeConfigurationUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public override EavAttributeType ValueType => EavAttributeType.DateRange;
        public bool IsSingleDate { get; set; }
    }
}