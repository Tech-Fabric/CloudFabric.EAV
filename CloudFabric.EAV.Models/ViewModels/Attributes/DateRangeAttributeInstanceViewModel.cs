namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class DateRangeAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public DateRangeAttributeInstanceValueViewModel Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}

public class DateRangeAttributeInstanceValueViewModel
{
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}
