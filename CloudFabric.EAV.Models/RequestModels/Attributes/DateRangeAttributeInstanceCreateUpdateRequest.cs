namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class DateRangeAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public DateTime Value { get; set; }
        public DateTime? Value2 { get; set; }
    }
}