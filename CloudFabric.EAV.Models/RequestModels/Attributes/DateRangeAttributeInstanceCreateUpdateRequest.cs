namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class DateRangeAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
    }
}