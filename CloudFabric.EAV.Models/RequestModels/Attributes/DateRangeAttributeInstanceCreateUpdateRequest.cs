namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class DateRangeAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
{
    public DateRangeAttributeInstanceValueRequest Value { get; set; }
}

public class DateRangeAttributeInstanceValueRequest
{
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}
