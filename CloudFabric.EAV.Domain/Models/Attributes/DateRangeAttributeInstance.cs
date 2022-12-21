namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class DateRangeAttributeInstance : AttributeInstance
    {
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
    }
}