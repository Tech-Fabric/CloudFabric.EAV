namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class NumberAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public decimal? Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}
