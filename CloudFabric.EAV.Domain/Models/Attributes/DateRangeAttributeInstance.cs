namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class DateRangeAttributeInstance : AttributeInstance
    {
        public DateTime From { get; set; }
        public DateTime? To { get; set; }

        public override object? GetValue()
        {
            return new List<DateTime?>() { From, To };
        }
    }
}