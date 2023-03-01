namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ImageAttributeInstance : AttributeInstance
    {
        public ImageAttributeValue Value { get; set; }
        public override object? GetValue()
        {
            return Value;
        }
    }
}
