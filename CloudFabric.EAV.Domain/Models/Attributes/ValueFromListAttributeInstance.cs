namespace CloudFabric.EAV.Domain.Models.Attributes;

public class ValueFromListAttributeInstance : AttributeInstance
{
    // value represented by one of options' machine names
    public string Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}
