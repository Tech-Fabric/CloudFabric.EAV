namespace CloudFabric.EAV.Domain.Models.Attributes;

public class MoneyAttributeInstance: AttributeInstance
{
    public decimal Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}
