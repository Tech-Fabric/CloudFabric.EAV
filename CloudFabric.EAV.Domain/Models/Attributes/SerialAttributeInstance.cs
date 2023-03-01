namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class SerialAttributeInstance : AttributeInstance
    {
        public long Value { get; set; }

        public override object? GetValue()
        {
            return Value;
        }
    }
}
