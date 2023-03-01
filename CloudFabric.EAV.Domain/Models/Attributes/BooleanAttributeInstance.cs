namespace CloudFabric.EAV.Domain.Models.Attributes;

public class BooleanAttributeInstance : AttributeInstance
{
    public bool Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}
