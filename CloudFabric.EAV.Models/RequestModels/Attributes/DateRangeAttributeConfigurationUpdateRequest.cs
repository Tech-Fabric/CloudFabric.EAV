using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class DateRangeAttributeConfigurationUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public override EavAttributeType ValueType => EavAttributeType.DateRange;
        public DateRangeAttributeType DateRangeAttributeType { get; set; }
    }
}