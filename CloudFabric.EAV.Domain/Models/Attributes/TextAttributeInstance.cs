namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class TextAttributeInstance : AttributeInstance
    {
        public string Value { get; set; }

        public override object? GetValue()
        {
            return Value;
        }
    }
}