namespace CloudFabric.EAV.Domain.Models.Attributes;

public class DateRangeAttributeInstanceValue
{
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}

public class DateRangeAttributeInstance : AttributeInstance
{
    public DateRangeAttributeInstanceValue Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}
